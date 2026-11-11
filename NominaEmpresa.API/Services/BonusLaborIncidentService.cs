
using Microsoft.EntityFrameworkCore;
using NominaEmpresa.API.Data;
using NominaEmpresa.API.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NominaEmpresa.API.Services;

public class BonusLaborIncidentService
{
    private readonly PayrollDbContext _context;

    public BonusLaborIncidentService(
        PayrollDbContext context)
    {
        _context = context;
    }

    public async Task<List<BonusLaborIncidentDto>>
        GetIncidentsAsync(
            DateOnly startDate,
            DateOnly endDate,
            List<int> contractIds)
    {
        var novelties = await _context.PayrollNovelties
            .AsNoTracking()
            .Include(n => n.Contract)
                .ThenInclude(c => c.Employee)
            .Where(n =>
                contractIds.Contains(n.ContractId) &&
                n.NoveltyDate >= startDate &&
                n.NoveltyDate <= endDate &&
                n.Category == "Informativa" &&
                n.Status != "Anulada")
            .ToListAsync();

        var results = new List<BonusLaborIncidentDto>();

        foreach (var novelty in novelties)
        {
            string treatment;
            string message;
            bool requiresReview = true;

            switch (novelty.NoveltyType.Trim()
                .ToLowerInvariant())
            {
                case "incapacidad":
                    treatment = "ConservaTiempo";

                    message =
                        "La incapacidad no reduce " +
                        "automáticamente los días de prima. " +
                        "Debe verificarse la base salarial " +
                        "y el período correspondiente.";
                    break;

                case "licencia":
                    treatment = "RevisarLicencia";

                    message =
                        "Identificar si la licencia es " +
                        "remunerada, de maternidad, " +
                        "paternidad o no remunerada. " +
                        "No descontar días automáticamente.";
                    break;

                case "ausencia":
                    treatment = "RevisarAusencia";

                    message =
                        "Verificar si corresponde a una " +
                        "ausencia justificada, una licencia " +
                        "o una suspensión formal.";
                    break;

                default:
                    treatment = "RevisionManual";

                    message =
                        "La novedad requiere clasificación " +
                        "laboral antes de aprobar la prima.";
                    break;
            }

            results.Add(new BonusLaborIncidentDto
            {
                NoveltyId = novelty.Id,
                ContractId = novelty.ContractId,

                EmployeeName =
                    $"{novelty.Contract.Employee.FirstName} " +
                    $"{novelty.Contract.Employee.LastName}",

                NoveltyDate = novelty.NoveltyDate,
                NoveltyType = novelty.NoveltyType,
                Description = novelty.Description,

                Treatment = treatment,
                Message = message,
                RequiresReview = requiresReview
            });
        }

        return results
            .OrderBy(x => x.EmployeeName)
            .ThenBy(x => x.NoveltyDate)
            .ToList();
    }
}
