using System.ComponentModel.DataAnnotations;

namespace NominaEmpresa.API.DTOs;

public class CreateContractDto
{
    [Range(1, int.MaxValue)]
    public int EmployeeId { get; set; }

    [Required]
    public string ContractType { get; set; } = "";

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Position { get; set; } = "";

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999.99")]
    public decimal MonthlySalary { get; set; }

    public DateOnly SalaryEffectiveFrom { get; set; }
}

public class ChangeSalaryDto
{
    [Range(typeof(decimal), "0.01", "9999999999999999.99")]
    public decimal MonthlySalary { get; set; }

    public DateOnly EffectiveFrom { get; set; }

    [StringLength(250)]
    public string? Reason { get; set; }
}

public class SalaryHistoryResponseDto
{
    public int Id { get; set; }

    public decimal MonthlySalary { get; set; }

    public DateOnly EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }

    public string? Reason { get; set; }
}

public class ContractResponseDto
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public string EmployeeName { get; set; } = "";

    public string Identification { get; set; } = "";

    public string ContractType { get; set; } = "";

    public string Position { get; set; } = "";

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public bool IsActive { get; set; }

    public decimal? CurrentMonthlySalary { get; set; }
}