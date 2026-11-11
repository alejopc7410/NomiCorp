
using System;

namespace NominaEmpresa.API.Models;

public class PayslipIssue
{
    public int Id { get; set; }

    public int PayrollDetailId { get; set; }

    public DateTime IssuedAtUtc { get; set; }
        = DateTime.UtcNow;

    public string? IssuedByUserId { get; set; }

    public bool IncludesBonus { get; set; }
}
