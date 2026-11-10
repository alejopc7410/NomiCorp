
using System.ComponentModel.DataAnnotations;

namespace NominaEmpresa.API.DTOs;

public class CreateEmployeeUserDto
{
    [Required]
    public int EmployeeId { get; set; }

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;
}

public class CreatedEmployeeUserDto
{
    public string UserId { get; set; } = string.Empty;
    public int EmployeeId { get; set; }
    public string Email { get; set; } = string.Empty;

    // Se muestra únicamente al crear la cuenta.
    public string TemporaryPassword { get; set; } = string.Empty;
}
