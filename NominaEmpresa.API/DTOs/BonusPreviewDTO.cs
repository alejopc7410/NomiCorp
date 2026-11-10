
using System.Collections.Generic;

namespace NominaEmpresa.API.DTOs;

public class BonusPreviewDetailDto
{
    public int ContractId { get; set; }
    public int EmployeeId { get; set; }

    public string EmployeeName { get; set; } = string.Empty;

    public string Identification { get; set; } = string.Empty;

    public decimal SalaryNovelties { get; set; }

    public int WorkedDays { get; set; }

    public decimal SalaryAccrued { get; set; }
    public decimal TransportAccrued { get; set; }

    public decimal EstimatedBonus { get; set; }
}

public class BonusPreviewDto
{
    public int Year { get; set; }
    public int Semester { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    public string Status { get; set; } = "Proyeccion";
    public string Message { get; set; } = string.Empty;

    public int EmployeeCount { get; set; }
    public decimal TotalBonus { get; set; }

    public List<BonusPreviewDetailDto> Details { get; set; }
        = new();
}
