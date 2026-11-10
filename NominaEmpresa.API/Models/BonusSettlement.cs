using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NominaEmpresa.API.Models;

public class BonusSettlement
{
    public int Id { get; set; }

    [Range(2020, 2100)]
    public int Year { get; set; }

    [Range(1, 2)]
    public int Semester { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    [Required, MaxLength(20)]
    public string Status { get; set; } = "Borrador";

    public decimal TotalAmount { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<BonusSettlementDetail> Details { get; set; } = new();
}
