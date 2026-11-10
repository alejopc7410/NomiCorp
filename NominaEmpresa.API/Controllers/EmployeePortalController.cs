using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NominaEmpresa.API.Authorization;
using NominaEmpresa.API.Data;
using NominaEmpresa.API.DTOs;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace NominaEmpresa.API.Controllers;

[ApiController]
[Route("api/employee-portal")]
[Authorize(Roles = "Empleado")]
[PasswordChangeRequired]
public class EmployeePortalController : ControllerBase
{
    private readonly PayrollDbContext _context;

    public EmployeePortalController(PayrollDbContext context)
    {
        _context = context;
    }

    [HttpGet("payslips")]
    public async Task<IActionResult> GetPayslips()
    {
        var employeeIdClaim = User.FindFirst("employeeId")?.Value;

        if (!int.TryParse(employeeIdClaim, out var employeeId))
        {
            return Unauthorized(new
            {
                Message = "No se pudo identificar al empleado."
            });
        }

        var payslips = await _context.PayrollDetails
            .AsNoTracking()
            .Where(d =>
                d.EmployeeId == employeeId &&
                d.PayrollPeriod.Status == "Cerrado"
            )
            .OrderByDescending(d => d.PayrollPeriod.Year)
            .ThenByDescending(d => d.PayrollPeriod.Month)
            .Select(d => new EmployeePayslipListDto
            {
                PayrollDetailId = d.Id,
                PeriodYear = d.PayrollPeriod.Year,
                PeriodMonth = d.PayrollPeriod.Month,
                StartDate = d.PayrollPeriod.StartDate,
                EndDate = d.PayrollPeriod.EndDate,
                TotalEarnings = d.TotalEarnings,
                TotalDeductions = d.TotalDeductions,
                NetPay = d.NetPay
            })
            .ToListAsync();

        return Ok(payslips);
    }

    [HttpGet("payslips/{id:int}")]
    public async Task<IActionResult> GetPayslip(int id)
    {
        var employeeIdClaim = User.FindFirst("employeeId")?.Value;

        if (!int.TryParse(employeeIdClaim, out var employeeId))
        {
            return Unauthorized(new
            {
                Message = "No se pudo identificar al empleado."
            });
        }

        var payslip = await _context.PayrollDetails
            .AsNoTracking()
            .Where(d =>
                d.Id == id &&
                d.EmployeeId == employeeId &&
                d.PayrollPeriod.Status == "Cerrado"
            )
            .Select(d => new EmployeePayslipDto
            {
                PayrollDetailId = d.Id,
                PeriodYear = d.PayrollPeriod.Year,
                PeriodMonth = d.PayrollPeriod.Month,
                StartDate = d.PayrollPeriod.StartDate,
                EndDate = d.PayrollPeriod.EndDate,

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
            .FirstOrDefaultAsync();

        if (payslip == null)
        {
            return NotFound(new
            {
                Message = "El desprendible no existe o no está disponible."
            });
        }

        return Ok(payslip);
    }

    [HttpGet("me")]
    public async Task<ActionResult<EmployeePortalDto>> GetMyInformation()
    {
        var employeeIdClaim = User.FindFirst("employeeId")?.Value;

        if (!int.TryParse(employeeIdClaim, out var employeeId))
        {
            return Unauthorized();
        }

        var employee = await _context.Employees
            .AsNoTracking()
            .Where(e => e.Id == employeeId && e.IsActive)
            .Select(e => new EmployeePortalDto
            {
                Id = e.Id,
                Identification = e.Identification,
                FirstName = e.FirstName,
                LastName = e.LastName,
                Email = e.Email,
                Phone = e.Phone,
                HireDate = e.HireDate
            })
            .FirstOrDefaultAsync();

        if (employee == null)
        {
            return NotFound("No se encontró el empleado.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        employee.ActiveContract = await _context.Contracts
            .AsNoTracking()
            .Where(c => c.EmployeeId == employeeId && c.IsActive)
            .OrderByDescending(c => c.StartDate)
            .Select(c => new ActiveContractDto
            {
                Id = c.Id,
                ContractType = c.ContractType,
                Position = c.Position,
                StartDate = c.StartDate,
                MonthlySalary = c.SalaryHistory
                    .Where(s => s.EffectiveFrom <= today &&
                        (s.EffectiveTo == null ||
                         s.EffectiveTo >= today))
                    .OrderByDescending(s => s.EffectiveFrom)
                    .Select(s => (decimal?)s.MonthlySalary)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        return Ok(employee);
    }
}