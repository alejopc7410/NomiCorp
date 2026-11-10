using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NominaEmpresa.API.Data;
using NominaEmpresa.API.DTOs;
using NominaEmpresa.API.Models;
using NominaEmpresa.API.Authorization;
using Microsoft.AspNetCore.Authorization;
namespace NominaEmpresa.API.Controllers;

[ApiController]
[Route("api/contracts")]
[PasswordChangeRequired]
[Authorize(Roles = "Administrador")]
public class ContractsController : ControllerBase
{
    private readonly PayrollDbContext _context;

    public ContractsController(PayrollDbContext context)
    {
        _context = context;
    }

    private static readonly HashSet<string> AllowedTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Indefinido",
            "Fijo",
            "Obra o labor"
        };

    // GET: api/contracts
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ContractResponseDto>>>
        GetContracts([FromQuery] int? employeeId = null)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var query = _context.Contracts
            .AsNoTracking()
            .AsQueryable();

        if (employeeId.HasValue)
        {
            query = query.Where(
                c => c.EmployeeId == employeeId.Value);
        }

        var contracts = await query
            .OrderByDescending(c => c.StartDate)
            .Select(c => new ContractResponseDto
            {
                Id = c.Id,
                EmployeeId = c.EmployeeId,
                EmployeeName = c.Employee.FirstName + " "
                    + c.Employee.LastName,
                Identification = c.Employee.Identification,
                ContractType = c.ContractType,
                Position = c.Position,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                IsActive = c.IsActive,
                CurrentMonthlySalary = c.SalaryHistory
                    .Where(s =>
                        s.EffectiveFrom <= today &&
                        (!s.EffectiveTo.HasValue ||
                         s.EffectiveTo.Value >= today))
                    .Select(s => (decimal?)s.MonthlySalary)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(contracts);
    }

    // POST: api/contracts
    [HttpPost]
    public async Task<ActionResult<ContractResponseDto>>
        CreateContract(CreateContractDto dto)
    {
        var type = dto.ContractType.Trim();

        if (!AllowedTypes.Contains(type))
        {
            return BadRequest(new
            {
                message = "Tipo de contrato no válido."
            });
        }

        if (dto.EndDate.HasValue &&
            dto.EndDate.Value < dto.StartDate)
        {
            return BadRequest(new
            {
                message = "La fecha final no puede ser anterior al inicio."
            });
        }

        if (dto.SalaryEffectiveFrom != dto.StartDate)
        {
            return BadRequest(new
            {
                message = "El salario inicial debe comenzar en la fecha de inicio del contrato."
            });
        }

        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == dto.EmployeeId);

        if (employee is null || !employee.IsActive)
        {
            return BadRequest(new
            {
                message = "El empleado no existe o está inactivo."
            });
        }

        // No permitir contratos laborales superpuestos
        // para el mismo empleado en esta primera versión.
        var overlapping = await _context.Contracts.AnyAsync(c =>
            c.EmployeeId == dto.EmployeeId &&
            c.StartDate <= (dto.EndDate ?? DateOnly.MaxValue) &&
            (!c.EndDate.HasValue ||
             c.EndDate.Value >= dto.StartDate));

        if (overlapping)
        {
            return Conflict(new
            {
                message = "El empleado ya tiene un contrato en ese intervalo."
            });
        }

        var contract = new Contract
        {
            EmployeeId = dto.EmployeeId,
            ContractType = type,
            Position = dto.Position.Trim(),
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            SalaryHistory = new List<SalaryHistory>
            {
                new()
                {
                    MonthlySalary = dto.MonthlySalary,
                    EffectiveFrom = dto.SalaryEffectiveFrom,
                    EffectiveTo = null,
                    Reason = "Salario inicial del contrato",
                    CreatedAtUtc = DateTime.UtcNow
                }
            }
        };

        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();

        var response = new ContractResponseDto
        {
            Id = contract.Id,
            EmployeeId = employee.Id,
            EmployeeName = employee.FirstName + " "
                + employee.LastName,
            Identification = employee.Identification,
            ContractType = contract.ContractType,
            Position = contract.Position,
            StartDate = contract.StartDate,
            EndDate = contract.EndDate,
            IsActive = contract.IsActive,
            CurrentMonthlySalary =
                dto.SalaryEffectiveFrom <= DateOnly.FromDateTime(DateTime.Today)
                    ? dto.MonthlySalary
                    : null
        };

        return CreatedAtAction(
            nameof(GetContracts),
            new { employeeId = employee.Id },
            response);
    }

    // GET: api/contracts/1/salary-history
    [HttpGet("{id:int}/salary-history")]
    public async Task<ActionResult<IEnumerable<SalaryHistoryResponseDto>>>
        GetSalaryHistory(int id)
    {
        var exists = await _context.Contracts
            .AnyAsync(c => c.Id == id);

        if (!exists)
            return NotFound();

        var history = await _context.SalaryHistories
            .AsNoTracking()
            .Where(s => s.ContractId == id)
            .OrderByDescending(s => s.EffectiveFrom)
            .Select(s => new SalaryHistoryResponseDto
            {
                Id = s.Id,
                MonthlySalary = s.MonthlySalary,
                EffectiveFrom = s.EffectiveFrom,
                EffectiveTo = s.EffectiveTo,
                Reason = s.Reason
            })
            .ToListAsync();

        return Ok(history);
    }

    // POST: api/contracts/1/salary-history
    [HttpPost("{id:int}/salary-history")]
    public async Task<IActionResult> ChangeSalary(
        int id,
        ChangeSalaryDto dto)
    {
        var contract = await _context.Contracts
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contract is null)
            return NotFound();

        if (!contract.IsActive)
        {
            return BadRequest(new
            {
                message = "No puedes cambiar el salario de un contrato inactivo."
            });
        }

        if (dto.EffectiveFrom < contract.StartDate ||
            (contract.EndDate.HasValue &&
             dto.EffectiveFrom > contract.EndDate.Value))
        {
            return BadRequest(new
            {
                message = "La vigencia salarial debe estar dentro del contrato."
            });
        }

        var latest = await _context.SalaryHistories
            .Where(s => s.ContractId == id)
            .OrderByDescending(s => s.EffectiveFrom)
            .FirstOrDefaultAsync();

        if (latest is null)
        {
            return Conflict(new
            {
                message = "El contrato no tiene salario inicial."
            });
        }

        if (dto.EffectiveFrom <= latest.EffectiveFrom)
        {
            return BadRequest(new
            {
                message = "La nueva vigencia debe ser posterior al último cambio salarial."
            });
        }

        if (latest.EffectiveTo.HasValue)
        {
            return Conflict(new
            {
                message = "El último periodo salarial ya está cerrado."
            });
        }

        // Cerrar el salario anterior el día previo al cambio.
        latest.EffectiveTo = dto.EffectiveFrom.AddDays(-1);

        var newSalary = new SalaryHistory
        {
            ContractId = id,
            MonthlySalary = dto.MonthlySalary,
            EffectiveFrom = dto.EffectiveFrom,
            EffectiveTo = null,
            Reason = string.IsNullOrWhiteSpace(dto.Reason)
                ? "Cambio salarial"
                : dto.Reason.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.SalaryHistories.Add(newSalary);

        await _context.SaveChangesAsync();

        return Created(
            $"/api/contracts/{id}/salary-history",
            new
            {
                newSalary.Id,
                newSalary.MonthlySalary,
                newSalary.EffectiveFrom,
                newSalary.EffectiveTo,
                newSalary.Reason
            });
    }

    // PATCH: api/contracts/1/end
    [HttpPatch("{id:int}/end")]
    public async Task<IActionResult> EndContract(
        int id,
        [FromQuery] DateOnly endDate)
    {
        var contract = await _context.Contracts
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contract is null)
            return NotFound();

        if (!contract.IsActive)
        {
            return BadRequest(new
            {
                message = "El contrato ya está inactivo."
            });
        }

        if (endDate < contract.StartDate ||
            (contract.EndDate.HasValue &&
             endDate > contract.EndDate.Value))
        {
            return BadRequest(new
            {
                message = "La fecha de terminación no es válida."
            });
        }

        var latest = await _context.SalaryHistories
            .Where(s => s.ContractId == id)
            .OrderByDescending(s => s.EffectiveFrom)
            .FirstOrDefaultAsync();

        if (latest is not null)
        {
            if (endDate < latest.EffectiveFrom)
            {
                return BadRequest(new
                {
                    message = "La fecha de terminación no puede ser anterior a la vigencia del último salario."
                });
            }

            latest.EffectiveTo = endDate;
        }

        contract.EndDate = endDate;
        contract.IsActive = false;
        contract.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }
}