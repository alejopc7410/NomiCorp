
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NominaEmpresa.API.Authorization;
using NominaEmpresa.API.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace NominaEmpresa.API.Controllers;

public class RegisterPreviousBonusPaymentDto
{
    [Range(1, int.MaxValue)]
    public int EmployeeId { get; set; }

    [Range(typeof(decimal), "0.01", "999999999999")]
    public decimal Amount { get; set; }

    [Required]
    public DateOnly? PaymentDate { get; set; }

    [Required, MaxLength(100)]
    public string Reference { get; set; }
        = string.Empty;

    [MaxLength(500)]
    public string? Notes { get; set; }
}

[ApiController]
[Route("api/bonus-settlements/{settlementId:int}/previous-payments")]
[Authorize(Roles = "Administrador")]
[PasswordChangeRequired]
public class BonusPreviousPaymentsController
    : ControllerBase
{
    private readonly BonusPaymentReconciliationService _service;

    public BonusPreviousPaymentsController(
        BonusPaymentReconciliationService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetSummary(
        int settlementId)
    {
        try
        {
            return Ok(
                await _service.GetSummaryAsync(
                    settlementId));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Register(
        int settlementId,
        RegisterPreviousBonusPaymentDto dto)
    {
        try
        {
            var payment = await _service.RegisterAsync(
                settlementId,
                dto.EmployeeId,
                dto.Amount,
                dto.PaymentDate!.Value,
                dto.Reference,
                dto.Notes);

            return Ok(new
            {
                message = "Pago anterior registrado.",
                payment.Id,
                payment.EmployeeId,
                payment.Amount,
                payment.PaymentDate,
                payment.Reference
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "No se pudo guardar el pago. " +
                          "Revisa si la referencia ya existe."
            });
        }
    }
}
