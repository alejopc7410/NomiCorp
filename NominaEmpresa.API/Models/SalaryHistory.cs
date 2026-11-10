using System.ComponentModel.DataAnnotations;

namespace NominaEmpresa.API.Models;

public class SalaryHistory
{
    public int Id { get; set; }

    public int ContractId { get; set; }

    public Contract Contract { get; set; } = null!;

    public decimal MonthlySalary { get; set; }

    public DateOnly EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }

    [MaxLength(250)]
    public string? Reason { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}