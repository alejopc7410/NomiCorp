
using System;
using System.ComponentModel.DataAnnotations;

namespace NominaEmpresa.API.Models;

public class BonusPreviousPayment
{
    public int Id { get; set; }

    public int BonusSettlementId { get; set; }

    public int EmployeeId { get; set; }

    [Range(typeof(decimal), "0.01", "999999999999")]
    public decimal Amount { get; set; }

    public DateOnly PaymentDate { get; set; }

    [Required, MaxLength(100)]
    public string Reference { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; }
        = DateTime.UtcNow;
}
