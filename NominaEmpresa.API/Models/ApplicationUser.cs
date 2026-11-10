
using Microsoft.AspNetCore.Identity;
using System;

namespace NominaEmpresa.API.Models;

public class ApplicationUser : IdentityUser
{
	// Relación con el empleado de la empresa
	public int? EmployeeId { get; set; }

	public Employee? Employee { get; set; }

	// Obliga al usuario a cambiar su contraseña inicial
	public bool MustChangePassword { get; set; } = true;

	public bool IsActive { get; set; } = true;

	public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
