using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NominaEmpresa.API.Authorization;
using NominaEmpresa.API.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NominaEmpresa.API.Controllers;

[ApiController]
[Route("api/bonus-settlements")]
[Authorize(Roles = "Administrador")]
[PasswordChangeRequired]
public class BonusSettlementsController : ControllerBase
{
    private readonly BonusSettlementService _service;
    private readonly BonusValidationService _validator;

    public BonusSettlementsController(BonusSettlementService service, BonusValidationService validator)
    {
        _service = service;
        _validator = validator;
    }


    [HttpGet("{id:int}/validate")]
    public async Task<IActionResult> Validate(int id)
    {
        try
        {
            var result = await _validator.ValidateAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
    }


    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var results = await _service.GetAllAsync();
        return Ok(results);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);

        if (result == null)
            return NotFound(new
            {
                message = "Liquidación no encontrada."
            });

        return Ok(result);
    }


    [HttpPut("{id:int}/cutoff")]
    public async Task<IActionResult> UpdateCutoff(
        int id,
        [FromBody] UpdateBonusCutoffDto dto)
    {
        try
        {
            var result =
                await _service.RecalculateDraftAsync(
                    id,
                    dto.CutoffDate
                );

            return Ok(new
            {
                message =
                    "Fecha de corte actualizada correctamente.",

                result.Id,
                result.Year,
                result.Semester,
                result.StartDate,
                result.EndDate,
                result.Status,
                result.TotalAmount,

                employeeCount = result.Details.Count
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }


    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateBonusSettlementDto dto)
    {
        try
        {
            var result = await _service.CreateDraftAsync(dto.Year, dto.Semester, dto.CutoffDate);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Id },
                new
                {
                    result.Id,
                    result.Year,
                    result.Semester,
                    result.Status,
                    result.TotalAmount
                });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            return Conflict(new
            {
                message = "No fue posible guardar la " +
                          "liquidación. Verifica si ya " +
                          "existe una para ese semestre."
            });
        }
    }


    [HttpPost("{id:int}/recalculate")]
    public async Task<IActionResult> Recalculate(int id)
    {
        try
        {
            var settlement =
                await _service.RecalculateDraftAsync(id);

            return Ok(new
            {
                message = "Liquidación recalculada correctamente.",
                settlement.Id,
                settlement.Year,
                settlement.Semester,
                settlement.Status,
                settlement.TotalAmount,
                employeeCount = settlement.Details.Count
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

}

public class CreateBonusSettlementDto
{
    [System.ComponentModel.DataAnnotations.Range(2020, 2100)]
    public int Year { get; set; }

    [System.ComponentModel.DataAnnotations.Range(1, 2)]
    public int Semester { get; set; }

    public DateOnly? CutoffDate { get; set; }
}

public class UpdateBonusCutoffDto
{
    [Required]
    public DateOnly? CutoffDate { get; set; }
}
