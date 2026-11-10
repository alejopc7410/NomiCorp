
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NominaEmpresa.API.Models;

public class PayrollPeriod
{
    public int Id { get; set; }

    public int Year { get; set; }

    public int Month { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    // Abierto, Calculado, Cerrado
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Abierto";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? ClosedAtUtc { get; set; }

    public ICollection<PayrollDetail> Details { get; set; }
        = new List<PayrollDetail>();
}
