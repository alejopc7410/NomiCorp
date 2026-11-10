using System.ComponentModel.DataAnnotations;

namespace NominaEmpresa.API.Models;

public class Contract
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    [Required]
    [MaxLength(30)]
    public string ContractType { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Position { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<SalaryHistory> SalaryHistory { get; set; }
        = new List<SalaryHistory>();
}