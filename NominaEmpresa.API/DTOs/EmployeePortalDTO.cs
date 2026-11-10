using System;

namespace NominaEmpresa.API.DTOs;

public class EmployeePortalDto
{
    public int Id { get; set; }
    public string Identification { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateOnly? HireDate { get; set; }

    public ActiveContractDto? ActiveContract { get; set; }
}

public class ActiveContractDto
{
    public int Id { get; set; }
    public string ContractType { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public decimal? MonthlySalary { get; set; }
}