using System.ComponentModel.DataAnnotations;

namespace NominaEmpresa.API.DTOs;

public class CreateEmployeeDto
{
    [Required]
    [StringLength(30, MinimumLength = 3)]
    public string Identification { get; set; } = "";

    [Required]
    [StringLength(80, MinimumLength = 2)]
    public string FirstName { get; set; } = "";

    [Required]
    [StringLength(80, MinimumLength = 2)]
    public string LastName { get; set; } = "";

    [EmailAddress]
    [StringLength(150)]
    public string? Email { get; set; }

    [StringLength(30)]
    public string? Phone { get; set; }

    public DateOnly? BirthDate { get; set; }

    public DateOnly? HireDate { get; set; }
}

public class UpdateEmployeeDto : CreateEmployeeDto
{
}

public class EmployeeResponseDto
{
    public int Id { get; set; }
    public string Identification { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateOnly? BirthDate { get; set; }
    public DateOnly? HireDate { get; set; }
    public bool IsActive { get; set; }
}