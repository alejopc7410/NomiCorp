
using System.Collections.Generic;

namespace NominaEmpresa.API.DTOs;

public class BonusValidationDto
{
    public int SettlementId { get; set; }

    public int Year { get; set; }

    public int Semester { get; set; }

    public bool ReadyForApproval { get; set; }

    public DateOnly ValidationDate { get; set; }

    public DateOnly PeriodEndDate { get; set; }

    public List<string> Errors { get; set; } = new();

    public List<string> Warnings { get; set; } = new();

    public List<BonusLaborIncidentDto> LaborIncidents { get; set; } = new();
}
