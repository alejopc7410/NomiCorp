
using System.ComponentModel.DataAnnotations;

namespace NominaEmpresa.API.Models;

public class PayrollConcept
{
    public int Id { get; set; }

    public int PayrollDetailId { get; set; }

    public PayrollDetail PayrollDetail { get; set; } = null!;

    [Required]
    [MaxLength(40)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string Description { get; set; } = string.Empty;

    // Devengado o Deduccion
    [Required]
    [MaxLength(20)]
    public string Category { get; set; } = string.Empty;

    public decimal? Quantity { get; set; }

    public decimal? UnitValue { get; set; }

    // Siempre almacenaremos el importe positivo.
    public decimal Amount { get; set; }
}
