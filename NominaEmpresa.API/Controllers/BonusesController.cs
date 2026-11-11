
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NominaEmpresa.API.Authorization;
using NominaEmpresa.API.Services;
using System;
using System.Threading.Tasks;

namespace NominaEmpresa.API.Controllers;

[ApiController]
[Route("api/bonuses")]
[Authorize(Roles = "Administrador")]
[PasswordChangeRequired]
public class BonusesController : ControllerBase
{
    private readonly BonusCalculationService _service;

    public BonusesController(
        BonusCalculationService service)
    {
        _service = service;
    }


    [HttpGet("preview")]
    public async Task<IActionResult> Preview(
        [FromQuery] int year,
        [FromQuery] int semester,
        [FromQuery] DateOnly? cutoffDate)
    {
        try
        {
            var result = await _service.PreviewAsync(
                year,
                semester,
                cutoffDate
            );

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [HttpGet("december-projection")]
    public async Task<IActionResult> DecemberProjection(
    [FromQuery] int year)
    {
        if (year < 2020 || year > 2100)
        {
            return BadRequest(new
            {
                message = "El año no es válido."
            });
        }

        try
        {
            var decemberEnd = new DateOnly(
                year, 12, 31
            );

            var result = await _service.PreviewAsync(
                year: year,
                semester: 2,
                cutoffDate: decemberEnd,
                allowFutureProjection: true
            );

            return Ok(result);
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
