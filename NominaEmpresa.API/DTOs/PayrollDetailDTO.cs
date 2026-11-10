using System.Collections.Generic;

namespace NominaEmpresa.API.DTOs;

public class PayrollDetailDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string Identification { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;

    public decimal MonthlySalary { get; set; }
    public decimal TotalEarnings { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetPay { get; set; }

    public List<PayrollConceptDto> Concepts { get; set; } = new();
}

public class PayrollConceptDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal? Quantity { get; set; }
    public decimal? UnitValue { get; set; }
    public decimal Amount { get; set; }
}