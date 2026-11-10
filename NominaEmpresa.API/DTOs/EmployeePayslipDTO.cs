using System.Collections.Generic;

namespace NominaEmpresa.API.DTOs;

public class EmployeePayslipListDto
{
    public int PayrollDetailId { get; set; }

    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    public decimal TotalEarnings { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetPay { get; set; }
}

public class EmployeePayslipDto
{
    public int PayrollDetailId { get; set; }

    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    public string Identification { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;

    public decimal MonthlySalary { get; set; }
    public decimal TotalEarnings { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetPay { get; set; }

    public List<PayrollConceptDto> Concepts { get; set; } = new();
}