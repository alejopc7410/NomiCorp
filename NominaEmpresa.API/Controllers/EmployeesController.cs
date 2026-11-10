using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NominaEmpresa.API.Data;
using NominaEmpresa.API.DTOs;
using NominaEmpresa.API.Models;
using NominaEmpresa.API.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace NominaEmpresa.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[PasswordChangeRequired]
[Authorize(Roles = "Administrador")]
public class EmployeesController : ControllerBase
{
    private readonly PayrollDbContext _context;

    public EmployeesController(PayrollDbContext context)
    {
        _context = context;
    }

    // GET: api/employees
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmployeeResponseDto>>>
        GetEmployees([FromQuery] bool? active = null)
    {
        var query = _context.Employees.AsNoTracking();

        if (active.HasValue)
        {
            query = query.Where(
                employee => employee.IsActive == active.Value);
        }

        var employees = await query
            .OrderBy(employee => employee.LastName)
            .ThenBy(employee => employee.FirstName)
            .Select(employee => new EmployeeResponseDto
            {
                Id = employee.Id,
                Identification = employee.Identification,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                Email = employee.Email,
                Phone = employee.Phone,
                BirthDate = employee.BirthDate,
                HireDate = employee.HireDate,
                IsActive = employee.IsActive
            })
            .ToListAsync();

        return Ok(employees);
    }

    // GET: api/employees/1
    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmployeeResponseDto>>
        GetEmployee(int id)
    {
        var employee = await _context.Employees
            .AsNoTracking()
            .Where(employee => employee.Id == id)
            .Select(employee => new EmployeeResponseDto
            {
                Id = employee.Id,
                Identification = employee.Identification,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                Email = employee.Email,
                Phone = employee.Phone,
                BirthDate = employee.BirthDate,
                HireDate = employee.HireDate,
                IsActive = employee.IsActive
            })
            .FirstOrDefaultAsync();

        if (employee is null)
            return NotFound();

        return Ok(employee);
    }

    // POST: api/employees
    [HttpPost]
    public async Task<ActionResult<EmployeeResponseDto>>
        CreateEmployee(CreateEmployeeDto dto)
    {
        var identification = dto.Identification.Trim();

        var exists = await _context.Employees.AnyAsync(
            employee => employee.Identification == identification);

        if (exists)
        {
            return Conflict(new
            {
                message = "Ya existe un empleado con esa identificación."
            });
        }

        if (dto.BirthDate.HasValue &&
            dto.BirthDate.Value > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return BadRequest(new
            {
                message = "La fecha de nacimiento no puede ser futura."
            });
        }

        if (dto.HireDate.HasValue &&
            dto.HireDate.Value > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return BadRequest(new
            {
                message = "La fecha de ingreso no puede ser futura."
            });
        }

        var employee = new Employee
        {
            Identification = identification,
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = NormalizeOptional(dto.Email),
            Phone = NormalizeOptional(dto.Phone),
            BirthDate = dto.BirthDate,
            HireDate = dto.HireDate,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        var response = new EmployeeResponseDto
        {
            Id = employee.Id,
            Identification = employee.Identification,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            Email = employee.Email,
            Phone = employee.Phone,
            BirthDate = employee.BirthDate,
            HireDate = employee.HireDate,
            IsActive = employee.IsActive
        };

        return CreatedAtAction(
            nameof(GetEmployee),
            new { id = employee.Id },
            response);
    }

    // PUT: api/employees/1
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateEmployee(
        int id,
        UpdateEmployeeDto dto)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(employee => employee.Id == id);

        if (employee is null)
            return NotFound();

        var identification = dto.Identification.Trim();

        var duplicate = await _context.Employees.AnyAsync(
            other => other.Id != id &&
                     other.Identification == identification);

        if (duplicate)
        {
            return Conflict(new
            {
                message = "Esa identificación ya pertenece a otro empleado."
            });
        }

        if (dto.BirthDate.HasValue &&
            dto.BirthDate.Value > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return BadRequest(new
            {
                message = "La fecha de nacimiento no puede ser futura."
            });
        }

        if (dto.HireDate.HasValue &&
            dto.HireDate.Value > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return BadRequest(new
            {
                message = "La fecha de ingreso no puede ser futura."
            });
        }

        employee.Identification = identification;
        employee.FirstName = dto.FirstName.Trim();
        employee.LastName = dto.LastName.Trim();
        employee.Email = NormalizeOptional(dto.Email);
        employee.Phone = NormalizeOptional(dto.Phone);
        employee.BirthDate = dto.BirthDate;
        employee.HireDate = dto.HireDate;
        employee.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // PATCH: api/employees/1/deactivate
    [HttpPatch("{id:int}/deactivate")]
    public async Task<IActionResult> DeactivateEmployee(int id)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(employee => employee.Id == id);

        if (employee is null)
            return NotFound();

        employee.IsActive = false;
        employee.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}