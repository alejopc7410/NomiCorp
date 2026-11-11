
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NominaEmpresa.API.Authorization;
using NominaEmpresa.API.Services;
using System;
using System.Threading.Tasks;

namespace NominaEmpresa.API.Controllers;

public class ScheduleBonusPayrollDto
{
    public int SettlementId { get; set; }
    public int PayrollPeriodId { get; set; }
}

[ApiController]
[Route("api/bonus-payroll")]
[Authorize(Roles = "Administrador")]
[PasswordChangeRequired]
public class BonusPayrollController : ControllerBase
{
    private readonly BonusPayrollIntegrationService _service;

    public BonusPayrollController(
        BonusPayrollIntegrationService service)
    {
        _service = service;
    }

    [HttpPost("schedule")]
    public async Task<IActionResult> Schedule(
        ScheduleBonusPayrollDto dto)
    {
        try
        {
            int count = await _service.ScheduleAsync(
                dto.SettlementId,
                dto.PayrollPeriodId);

            return Ok(new
            {
                message = "Prima programada en nómina.",
                employees = count
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
