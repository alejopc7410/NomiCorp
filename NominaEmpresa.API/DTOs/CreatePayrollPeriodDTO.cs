using System.ComponentModel.DataAnnotations;

namespace NominaEmpresa.API.DTOs;

public class CreatePayrollPeriodDto
{
    [Range(2020, 2100)]
    public int Year { get; set; }

    [Range(1, 12)]
    public int Month { get; set; }
}
