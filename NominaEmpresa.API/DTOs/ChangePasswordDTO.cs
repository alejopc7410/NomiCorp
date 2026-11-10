
using System.ComponentModel.DataAnnotations;

namespace NominaEmpresa.API.DTOs;

public class ChangePasswordDto
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(10)]
    public string NewPassword { get; set; } = string.Empty;
}
