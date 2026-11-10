
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NominaEmpresa.API.Authorization;
using NominaEmpresa.API.Data;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace NominaEmpresa.API.Controllers;

public class ClassifyPayrollNoveltyDto
{
    [Required]
    public bool? IsSalaryConstitutive { get; set; }
}

[ApiController]
[Route("api/payroll-novelty-classification")]
[Authorize(Roles = "Administrador")]
[PasswordChangeRequired]
public class PayrollNoveltyClassificationController : ControllerBase
{
    private readonly PayrollDbContext _context;

    public PayrollNoveltyClassificationController(PayrollDbContext context)
    {
        _context = context;
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Classify(int id, ClassifyPayrollNoveltyDto dto)
    {
        var novelty = await _context.PayrollNovelties
            .FirstOrDefaultAsync(n => n.Id == id);

        if (novelty == null)
        {
            return NotFound(new
            {
                message = "La novedad no existe."
            });
        }

        if (novelty.Category != "Devengado")
        {
            return BadRequest(new
            {
                message = "Solo se clasifican como " +
                          "salariales los conceptos devengados."
            });
        }

        var periodIsClosed = await _context.PayrollPeriods
            .AnyAsync(p =>
                p.Year == novelty.PeriodYear &&
                p.Month == novelty.PeriodMonth &&
                p.Status == "Cerrado");

        if (periodIsClosed)
        {
            return Conflict(new
            {
                message = "No se puede modificar una novedad " +
                          "de una nómina cerrada."
            });
        }

        novelty.IsSalaryConstitutive =
            dto.IsSalaryConstitutive!.Value;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            novelty.Id,
            novelty.Description,
            novelty.Category,
            novelty.IsSalaryConstitutive
        });
    }
}
