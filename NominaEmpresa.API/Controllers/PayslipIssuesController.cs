
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NominaEmpresa.API.Authorization;
using NominaEmpresa.API.Data;
using NominaEmpresa.API.Models;
using System.Security.Claims;
using System.Threading.Tasks;

namespace NominaEmpresa.API.Controllers;

[ApiController]
[Route("api/payslip-issues")]
[Authorize(Roles = "Administrador")]
[PasswordChangeRequired]
public class PayslipIssuesController : ControllerBase
{
    private readonly PayrollDbContext _context;

    public PayslipIssuesController(PayrollDbContext context)
    {
        _context = context;
    }

    [HttpPost("{payrollDetailId:int}")]
    public async Task<IActionResult> Register(
        int payrollDetailId)
    {
        var detail = await _context.PayrollDetails
            .Include(d => d.Concepts)
            .FirstOrDefaultAsync(d => d.Id == payrollDetailId);

        if (detail == null)
        {
            return NotFound(new
            {
                message = "Detalle de nómina no encontrado."
            });
        }

        var issue = new PayslipIssue
        {
            PayrollDetailId = detail.Id,
            IssuedByUserId =
                User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub"),

            IncludesBonus = detail.Concepts
                .Any(c => c.Code == "PRI-SERV")
        };

        _context.PayslipIssues.Add(issue);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            issue.Id,
            issue.PayrollDetailId,
            issue.IssuedAtUtc,
            issue.IncludesBonus
        });
    }
}
