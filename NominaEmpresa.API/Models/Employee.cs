using System.ComponentModel.DataAnnotations;

namespace NominaEmpresa.API.Models;

public class Employee
{
    public int Id { get; set; }

    [Required]
    [MaxLength(30)]
    public string Identification { get; set; } = string.Empty;

    [Required]
    [MaxLength(80)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(80)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(150)]
    [EmailAddress]
    public string? Email { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }

    public DateOnly? BirthDate { get; set; }

    public DateOnly? HireDate { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }
        = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}