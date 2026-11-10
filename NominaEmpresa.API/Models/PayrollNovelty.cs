
using System.ComponentModel.DataAnnotations;

namespace NominaEmpresa.API.Models;

public class PayrollNovelty
{
    public int Id { get; set; }

    public int ContractId { get; set; }
    public Contract Contract { get; set; } = null!;

    // Periodo al que pertenece la novedad
    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; }

    public DateOnly NoveltyDate { get; set; }

    [Required, MaxLength(40)]
    public string NoveltyType { get; set; } = string.Empty;

    // Devengado, Deduccion o Informativa
    [Required, MaxLength(20)]
    public string Category { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    public decimal? Quantity { get; set; }

    public decimal? UnitValue { get; set; }

    // Importe positivo; la categoría determina su tratamiento
    public decimal Amount { get; set; }

    // Pendiente, Aprobada o Anulada
    [Required, MaxLength(20)]
    public string Status { get; set; } = "Pendiente";

    [MaxLength(500)]
    public string? Notes { get; set; }

    public bool? IsSalaryConstitutive { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
