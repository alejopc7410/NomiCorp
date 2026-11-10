
using System.ComponentModel.DataAnnotations;

namespace NominaEmpresa.API.DTOs;

public class CreatePayrollNoveltyDto
{
    [Range(1, int.MaxValue)]
    public int ContractId { get; set; }

    [Range(2020, 2100)]
    public int PeriodYear { get; set; }

    [Range(1, 12)]
    public int PeriodMonth { get; set; }

    public DateOnly NoveltyDate { get; set; }

    [Required, MaxLength(40)]
    public string NoveltyType { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Category { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.0001", "999999999999")]
    public decimal? Quantity { get; set; }

    [Range(typeof(decimal), "0.01", "999999999999")]
    public decimal? UnitValue { get; set; }

    [Range(typeof(decimal), "0.01", "999999999999")]
    public decimal Amount { get; set; }

    // Obligatorio para devengados; null para las otras categorias.
    public bool? IsSalaryConstitutive { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class UpdatePayrollNoveltyDto : CreatePayrollNoveltyDto
{
}

public class PayrollNoveltyResponseDto
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string Identification { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; }
    public DateOnly NoveltyDate { get; set; }
    public string NoveltyType { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal? Quantity { get; set; }
    public decimal? UnitValue { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool? IsSalaryConstitutive { get; set; }
    public string? Notes { get; set; }
}
