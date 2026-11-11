
using System;
using System.ComponentModel.DataAnnotations;

namespace NominaEmpresa.API.Models;

public class BonusPayrollAllocation
{
    public int Id { get; set; }

    public int BonusSettlementDetailId { get; set; }

    public int PayrollPeriodId { get; set; }

    public int EmployeeId { get; set; }

    public decimal Amount { get; set; }

    [Required, MaxLength(30)]
    public string Status { get; set; } = "Programada";

    public DateTime CreatedAtUtc { get; set; }
        = DateTime.UtcNow;

    public DateTime? IncludedAtUtc { get; set; }
}
