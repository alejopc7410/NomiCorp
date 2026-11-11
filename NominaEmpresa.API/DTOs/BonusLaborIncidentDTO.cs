
namespace NominaEmpresa.API.DTOs;

public class BonusLaborIncidentDto
{
    public int NoveltyId { get; set; }

    public int ContractId { get; set; }

    public string EmployeeName { get; set; }
        = string.Empty;

    public DateOnly NoveltyDate { get; set; }

    public string NoveltyType { get; set; }
        = string.Empty;

    public string Description { get; set; }
        = string.Empty;

    public string Treatment { get; set; }
        = string.Empty;

    public string Message { get; set; }
        = string.Empty;

    public bool RequiresReview { get; set; }
}
