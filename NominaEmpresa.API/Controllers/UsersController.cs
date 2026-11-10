
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NominaEmpresa.API.Authorization;
using NominaEmpresa.API.Data;
using NominaEmpresa.API.DTOs;
using NominaEmpresa.API.Models;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace NominaEmpresa.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador")]
[PasswordChangeRequired]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly PayrollDbContext _context;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        PayrollDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    [HttpPost("employee")]
    public async Task<ActionResult<CreatedEmployeeUserDto>> CreateEmployeeUser(
        CreateEmployeeUserDto dto)
    {
        var email = dto.Email.Trim();

        // 1. Comprobar que el empleado exista y esté activo.
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e =>
                e.Id == dto.EmployeeId && e.IsActive);

        if (employee == null)
        {
            return NotFound(new
            {
                message = "El empleado no existe o está inactivo."
            });
        }

        // 2. Comprobar que el empleado no tenga ya una cuenta.
        var employeeHasUser = await _context.Users
            .AnyAsync(u => u.EmployeeId == dto.EmployeeId);

        if (employeeHasUser)
        {
            return Conflict(new
            {
                message = "Este empleado ya tiene una cuenta de usuario."
            });
        }

        // 3. Comprobar que el correo no esté registrado.
        var existingUser = await _userManager.FindByEmailAsync(email);

        if (existingUser != null)
        {
            return Conflict(new
            {
                message = "Este correo electrónico ya está registrado."
            });
        }

        // 4. Generar contraseña temporal segura.
        var temporaryPassword = GenerateTemporaryPassword();

        // 5. Crear el usuario.
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmployeeId = employee.Id,
            MustChangePassword = true,
            IsActive = true,
            EmailConfirmed = false
        };

        var createResult = await _userManager.CreateAsync(
            user,
            temporaryPassword
        );

        if (!createResult.Succeeded)
        {
            return BadRequest(new
            {
                errors = createResult.Errors.Select(e => e.Description)
            });
        }

        // 6. Asignar el rol Empleado.
        var roleResult = await _userManager.AddToRoleAsync(
            user,
            "Empleado"
        );

        if (!roleResult.Succeeded)
        {
            // Evitar dejar una cuenta creada sin el rol esperado.
            await _userManager.DeleteAsync(user);

            return StatusCode(500, new
            {
                message = "No se pudo asignar el rol al usuario."
            });
        }

        // 7. Devolver la contraseña temporal una sola vez.
        return StatusCode(
            StatusCodes.Status201Created, 
            new CreatedEmployeeUserDto {
                UserId = user.Id,
                EmployeeId = employee.Id,
                Email = user.Email!,
                TemporaryPassword = temporaryPassword
            }
        );
    }

    private static string GenerateTemporaryPassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnopqrstuvwxyz";
        const string digits = "23456789";
        const string special = "!@#$%&*?";

        const string all = upper + lower + digits + special;

        var password = new List<char>
        {
            upper[RandomNumberGenerator.GetInt32(upper.Length)],
            lower[RandomNumberGenerator.GetInt32(lower.Length)],
            digits[RandomNumberGenerator.GetInt32(digits.Length)],
            special[RandomNumberGenerator.GetInt32(special.Length)]
        };

        while (password.Count < 16)
        {
            password.Add(all[RandomNumberGenerator.GetInt32(all.Length)]);
        }

        // Mezclar los caracteres aleatoriamente.
        for (int i = password.Count - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            (password[i], password[j]) = (password[j], password[i]);
        }

        return new string(password.ToArray());
    }
}
