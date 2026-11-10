
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NominaEmpresa.API.Authorization;
using NominaEmpresa.API.Data;
using NominaEmpresa.API.DTOs;
using NominaEmpresa.API.Models;
using NominaEmpresa.API.Services;
using System;
using System.Threading.Tasks;

namespace NominaEmpresa.API.Controllers;

[ApiController]
[Route("api/payroll")]
[Authorize(Roles = "Administrador")]
[PasswordChangeRequired]
public class PayrollController : ControllerBase
{
    private readonly PayrollDbContext _context;
    private readonly PayrollCalculationService _calculationService;

    public PayrollController(
        PayrollDbContext context,
        PayrollCalculationService calculationService)
    {
        _context = context;
        _calculationService = calculationService;
    }

    [HttpGet("periods/{id:int}/details")]
    public async Task<IActionResult> GetPeriodDetails(int id)
    {
        var period = await _context.PayrollPeriods
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (period == null)
        {
            return NotFound(new
            {
                Message = "El período de nómina no existe."
            });
        }

        var details = await _context.PayrollDetails
            .AsNoTracking()
            .Where(d => d.PayrollPeriodId == id)
            .OrderBy(d => d.EmployeeName)
            .Select(d => new PayrollDetailDto
            {
                Id = d.Id,
                EmployeeId = d.EmployeeId,
                Identification = d.Identification,
                EmployeeName = d.EmployeeName,
                Position = d.Position,
                MonthlySalary = d.MonthlySalary,
                TotalEarnings = d.TotalEarnings,
                TotalDeductions = d.TotalDeductions,
                NetPay = d.NetPay,

                Concepts = d.Concepts.Select(c => new PayrollConceptDto
                {
                    Id = c.Id,
                    Code = c.Code,
                    Description = c.Description,
                    Category = c.Category,
                    Quantity = c.Quantity,
                    UnitValue = c.UnitValue,
                    Amount = c.Amount
                }).ToList()
            })
            .ToListAsync();

        return Ok(new
        {
            Period = new
            {
                period.Id,
                period.Year,
                period.Month,
                period.StartDate,
                period.EndDate,
                period.Status
            },

            EmployeeCount = details.Count,
            TotalEarnings = details.Sum(d => d.TotalEarnings),
            TotalDeductions = details.Sum(d => d.TotalDeductions),
            TotalNetPay = details.Sum(d => d.NetPay),

            Details = details
        });
    }

    [HttpPost("periods/{id:int}/close")]
    public async Task<IActionResult> ClosePeriod(int id)
    {
        var period = await _context.PayrollPeriods
            .FirstOrDefaultAsync(p => p.Id == id);

        if (period == null)
        {
            return NotFound(new
            {
                Message = "El período de nómina no existe."
            });
        }

        // Solo se pueden cerrar períodos calculados.
        if (period.Status != "Calculado")
        {
            return BadRequest(new
            {
                Message = "Solo se pueden cerrar períodos que hayan sido calculados."
            });
        }

        var details = await _context.PayrollDetails
            .AsNoTracking()
            .Where(d => d.PayrollPeriodId == id)
            .ToListAsync();

        // No permitir cerrar una nómina vacía.
        if (details.Count == 0)
        {
            return BadRequest(new
            {
                Message = "No se puede cerrar una nómina sin empleados procesados."
            });
        }

        // Verificar que los valores netos sean consistentes.
        var invalidDetails = details.Any(d =>
            d.NetPay != d.TotalEarnings - d.TotalDeductions
        );

        if (invalidDetails)
        {
            return BadRequest(new
            {
                Message = "Existen inconsistencias en los valores de la nómina."
            });
        }

        period.Status = "Cerrado";
        period.ClosedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Message = "La nómina fue cerrada correctamente.",
            PeriodId = period.Id,
            Status = period.Status,
            ClosedAtUtc = period.ClosedAtUtc,
            EmployeeCount = details.Count,
            TotalEarnings = details.Sum(d => d.TotalEarnings),
            TotalDeductions = details.Sum(d => d.TotalDeductions),
            TotalNetPay = details.Sum(d => d.NetPay)
        });
    }

    // Consultar todos los periodos
    [HttpGet("periods")]
    public async Task<IActionResult> GetPeriods()
    {
        var periods = await _context.PayrollPeriods
            .AsNoTracking()
            .OrderByDescending(p => p.Year)
            .ThenByDescending(p => p.Month)
            .Select(p => new
            {
                p.Id,
                p.Year,
                p.Month,
                p.StartDate,
                p.EndDate,
                p.Status,
                p.CreatedAtUtc,
                p.ClosedAtUtc,
                EmployeeCount = p.Details.Count
            })
            .ToListAsync();

        return Ok(periods);
    }

    // Crear un periodo mensual
    [HttpPost("periods")]
    public async Task<IActionResult> CreatePeriod(
        CreatePayrollPeriodDto dto)
    {
        var exists = await _context.PayrollPeriods
            .AnyAsync(p =>
                p.Year == dto.Year &&
                p.Month == dto.Month);

        if (exists)
        {
            return Conflict(
                "Ya existe un periodo de nómina para ese año y mes.");
        }

        var startDate = new DateOnly(dto.Year, dto.Month, 1);

        var endDate = new DateOnly(
            dto.Year,
            dto.Month,
            DateTime.DaysInMonth(dto.Year, dto.Month));

        var period = new PayrollPeriod
        {
            Year = dto.Year,
            Month = dto.Month,
            StartDate = startDate,
            EndDate = endDate,
            Status = "Abierto"
        };

        _context.PayrollPeriods.Add(period);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetPeriods),
            new { },
            new
            {
                period.Id,
                period.Year,
                period.Month,
                period.StartDate,
                period.EndDate,
                period.Status
            });
    }

    // Ejecutar liquidación
    [HttpPost("periods/{id:int}/calculate")]
    public async Task<IActionResult> CalculatePeriod(int id)
    {
        try
        {
            var processedCount =
                await _calculationService.CalculatePeriodAsync(id);

            return Ok(new
            {
                Message = "La nómina fue calculada correctamente.",
                PeriodId = id,
                ProcessedContracts = processedCount,
                Status = "Calculado"
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                Message = ex.Message
            });
        }
    }
}
