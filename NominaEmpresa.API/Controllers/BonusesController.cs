
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
}
