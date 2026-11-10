
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NominaEmpresa.API.Data;
using NominaEmpresa.API.DTOs;
using NominaEmpresa.API.Models;
using NominaEmpresa.API.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace NominaEmpresa.API.Controllers;

[ApiController]
[Route("api/payroll-novelties")]
[PasswordChangeRequired]
[Authorize(Roles = "Administrador")]
public class PayrollNoveltiesController : ControllerBase
{
    private readonly PayrollDbContext _context;

    private static readonly string[] AllowedTypes =
    [
        "Horas extra",
        "Recargo nocturno",
        "Recargo dominical/festivo",
        "Bonificacion",
        "Comision",
        "Auxilio",
        "Incapacidad",
        "Licencia",
        "Ausencia",
        "Deduccion",
        "Otro"
    ];

    private static readonly string[] AllowedCategories =
    [
        "Devengado",
        "Deduccion",
        "Informativa"
    ];

    public PayrollNoveltiesController(PayrollDbContext context)
    {
        _context = context;
    }

    // GET: api/payroll-novelties?year=2026&month=10
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PayrollNoveltyResponseDto>>> GetAll(
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] int? contractId,
        [FromQuery] string? status)
    {
        if (year.HasValue && (year < 2020 || year > 2100))
            return BadRequest("El año no es válido.");

        if (month.HasValue && (month < 1 || month > 12))
            return BadRequest("El mes debe estar entre 1 y 12.");

        var query = _context.PayrollNovelties
            .AsNoTracking()
            .Include(n => n.Contract)
                .ThenInclude(c => c.Employee)
            .AsQueryable();

        if (year.HasValue)
            query = query.Where(n => n.PeriodYear == year.Value);

        if (month.HasValue)
            query = query.Where(n => n.PeriodMonth == month.Value);

        if (contractId.HasValue)
            query = query.Where(n => n.ContractId == contractId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(n => n.Status == status);

        var results = await query
            .OrderByDescending(n => n.PeriodYear)
            .ThenByDescending(n => n.PeriodMonth)
            .ThenBy(n => n.NoveltyDate)
            .Select(n => new PayrollNoveltyResponseDto
            {
                Id = n.Id,
                ContractId = n.ContractId,
                EmployeeName = n.Contract.Employee.FirstName
                    + " " + n.Contract.Employee.LastName,
                Identification = n.Contract.Employee.Identification,
                Position = n.Contract.Position,
                PeriodYear = n.PeriodYear,
                PeriodMonth = n.PeriodMonth,
                NoveltyDate = n.NoveltyDate,
                NoveltyType = n.NoveltyType,
                Category = n.Category,
                Description = n.Description,
                Quantity = n.Quantity,
                UnitValue = n.UnitValue,
                Amount = n.Amount,
                Status = n.Status,
                IsSalaryConstitutive = n.IsSalaryConstitutive,
                Notes = n.Notes
            })
            .ToListAsync();

        return Ok(results);
    }

    // POST: api/payroll-novelties
    [HttpPost]
    public async Task<ActionResult<PayrollNoveltyResponseDto>> Create(
        CreatePayrollNoveltyDto dto)
    {
        var validationError = ValidateDto(dto);
        if (validationError is not null)
            return BadRequest(validationError);

        var contract = await _context.Contracts
            .Include(c => c.Employee)
            .FirstOrDefaultAsync(c => c.Id == dto.ContractId);

        if (contract is null)
            return BadRequest("El contrato indicado no existe.");

        if (!contract.IsActive)
            return BadRequest("No puedes registrar novedades en un contrato inactivo.");

        if (dto.NoveltyDate < contract.StartDate ||
            (contract.EndDate.HasValue &&
             dto.NoveltyDate > contract.EndDate.Value))
        {
            return BadRequest(
                "La fecha de la novedad debe estar dentro de la vigencia del contrato.");
        }

        if (dto.NoveltyDate.Year != dto.PeriodYear ||
            dto.NoveltyDate.Month != dto.PeriodMonth)
        {
            return BadRequest(
                "La fecha de la novedad debe coincidir con el período seleccionado.");
        }

        var novelty = new PayrollNovelty
        {
            ContractId = dto.ContractId,
            PeriodYear = dto.PeriodYear,
            PeriodMonth = dto.PeriodMonth,
            NoveltyDate = dto.NoveltyDate,
            NoveltyType = dto.NoveltyType.Trim(),
            Category = dto.Category.Trim(),
            Description = dto.Description.Trim(),
            Quantity = dto.Quantity,
            UnitValue = dto.UnitValue,
            Amount = dto.Amount,
            Notes = dto.Notes?.Trim(),
            IsSalaryConstitutive = dto.Category.Trim() == "Devengado"
                ? dto.IsSalaryConstitutive : null,
            Status = "Pendiente"
        };

        _context.PayrollNovelties.Add(novelty);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetAll),
            new { year = novelty.PeriodYear, month = novelty.PeriodMonth },
            new
            {
                novelty.Id,
                Message = "Novedad registrada correctamente."
            });
    }

    // PUT: api/payroll-novelties/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        UpdatePayrollNoveltyDto dto)
    {
        var validationError = ValidateDto(dto);
        if (validationError is not null)
            return BadRequest(validationError);

        var novelty = await _context.PayrollNovelties
            .Include(n => n.Contract)
            .FirstOrDefaultAsync(n => n.Id == id);

        if (novelty is null)
            return NotFound("No se encontró la novedad.");

        if (novelty.Status != "Pendiente")
            return BadRequest(
                "Solo puedes editar novedades que estén pendientes.");

        if (novelty.ContractId != dto.ContractId)
            return BadRequest(
                "No se puede cambiar el contrato de una novedad existente.");

        if (dto.NoveltyDate < novelty.Contract.StartDate ||
            (novelty.Contract.EndDate.HasValue &&
             dto.NoveltyDate > novelty.Contract.EndDate.Value))
        {
            return BadRequest(
                "La fecha debe estar dentro de la vigencia del contrato.");
        }

        if (dto.NoveltyDate.Year != dto.PeriodYear ||
            dto.NoveltyDate.Month != dto.PeriodMonth)
        {
            return BadRequest(
                "La fecha de la novedad debe coincidir con el período seleccionado.");
        }

        var error = ValidateTypeAndCategory(dto.NoveltyType, dto.Category);
        if (error is not null)
            return BadRequest(error);

        novelty.PeriodYear = dto.PeriodYear;
        novelty.PeriodMonth = dto.PeriodMonth;
        novelty.NoveltyDate = dto.NoveltyDate;
        novelty.NoveltyType = dto.NoveltyType.Trim();
        novelty.Category = dto.Category.Trim();
        novelty.Description = dto.Description.Trim();
        novelty.Quantity = dto.Quantity;
        novelty.UnitValue = dto.UnitValue;
        novelty.Amount = dto.Amount;
        novelty.Notes = dto.Notes?.Trim();
        novelty.IsSalaryConstitutive = dto.Category.Trim() == "Devengado"
            ? dto.IsSalaryConstitutive : null;
        novelty.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // PATCH: api/payroll-novelties/5/approve
    [HttpPatch("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        var novelty = await _context.PayrollNovelties
            .FirstOrDefaultAsync(n => n.Id == id);

        if (novelty is null)
            return NotFound("No se encontró la novedad.");

        if (novelty.Status != "Pendiente")
            return BadRequest("Solo se pueden aprobar novedades pendientes.");

        if (novelty.Category == "Devengado" &&
            !novelty.IsSalaryConstitutive.HasValue)
        {
            return BadRequest(
                "Clasifica si el devengado constituye salario antes de aprobarlo.");
        }

        novelty.Status = "Aprobada";
        novelty.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // PATCH: api/payroll-novelties/5/cancel
    [HttpPatch("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var novelty = await _context.PayrollNovelties
            .FirstOrDefaultAsync(n => n.Id == id);

        if (novelty is null)
            return NotFound("No se encontró la novedad.");

        if (novelty.Status == "Anulada")
            return BadRequest("La novedad ya está anulada.");

        novelty.Status = "Anulada";
        novelty.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static string? ValidateDto(CreatePayrollNoveltyDto dto)
    {
        var typeError = ValidateTypeAndCategory(
            dto.NoveltyType, dto.Category);

        if (typeError is not null)
            return typeError;

        if (string.IsNullOrWhiteSpace(dto.Description))
            return "La descripción es obligatoria.";

        if (dto.Amount <= 0)
            return "El valor debe ser mayor que cero.";

        if (dto.Quantity.HasValue && dto.Quantity.Value <= 0)
            return "La cantidad debe ser mayor que cero.";

        if (dto.UnitValue.HasValue && dto.UnitValue.Value <= 0)
            return "El valor unitario debe ser mayor que cero.";

        if (dto.NoveltyDate == default)
            return "La fecha de la novedad es obligatoria.";

        if (dto.NoveltyDate.Year != dto.PeriodYear ||
            dto.NoveltyDate.Month != dto.PeriodMonth)
        {
            return "La fecha debe pertenecer al período seleccionado.";
        }

        return null;
    }

    private static string? ValidateTypeAndCategory(
        string? type,
        string? category)
    {
        if (string.IsNullOrWhiteSpace(type) ||
            !AllowedTypes.Contains(type.Trim(),
                StringComparer.OrdinalIgnoreCase))
        {
            return "El tipo de novedad no es válido.";
        }

        if (string.IsNullOrWhiteSpace(category) ||
            !AllowedCategories.Contains(category.Trim(),
                StringComparer.OrdinalIgnoreCase))
        {
            return "La categoría debe ser Devengado, Deduccion o Informativa.";
        }

        return null;
    }
}
