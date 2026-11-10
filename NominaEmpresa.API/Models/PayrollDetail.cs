
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Contracts;

namespace NominaEmpresa.API.Models;

public class PayrollDetail
{
    public int Id { get; set; }

    public int PayrollPeriodId { get; set; }

    public PayrollPeriod PayrollPeriod { get; set; } = null!;

    public int EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    public int ContractId { get; set; }

    public Contract Contract { get; set; } = null!;

    // Datos históricos del empleado al liquidar
    [Required]
    [MaxLength(30)]
    public string Identification { get; set; } = string.Empty;

    [Required]
    [MaxLength(161)]
    public string EmployeeName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Position { get; set; } = string.Empty;

    // Salario vigente al momento de liquidar
    public decimal MonthlySalary { get; set; }

    public decimal TotalEarnings { get; set; }

    public decimal TotalDeductions { get; set; }

    public decimal NetPay { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<PayrollConcept> Concepts { get; set; }
        = new List<PayrollConcept>();
}
