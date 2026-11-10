
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NominaEmpresa.API.DTOs;

public class LoginDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public string Email { get; set; } = string.Empty;

    public IList<string> Roles { get; set; } = new List<string>();

    public bool MustChangePassword { get; set; }

    public int? EmployeeId { get; set; }
}
