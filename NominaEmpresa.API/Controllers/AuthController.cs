using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NominaEmpresa.API.DTOs;
using NominaEmpresa.API.Models;
using NominaEmpresa.API.Services;
using System;
using System.Threading.Tasks;

namespace NominaEmpresa.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly TokenService _tokenService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        TokenService tokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginDto dto)
    {
        // Buscar usuario por correo.
        var user = await _userManager.FindByEmailAsync(dto.Email);

        if (user == null || !user.IsActive)
        {
            return Unauthorized(new
            {
                message = "Correo o contraseña incorrectos."
            });
        }

        // Validar contraseña y aplicar bloqueo por intentos fallidos.
        var passwordResult = await _signInManager
            .CheckPasswordSignInAsync(
                user,
                dto.Password,
                lockoutOnFailure: true
            );

        if (!passwordResult.Succeeded)
        {
            return Unauthorized(new
            {
                message = "Correo o contraseña incorrectos."
            });
        }

        // Obtener los roles del usuario.
        var roles = await _userManager.GetRolesAsync(user);

        // Generar JWT.
        var token = _tokenService.CreateToken(user, roles);

        return Ok(new LoginResponseDto
        {
            Token = token,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(30),
            Email = user.Email ?? "",
            Roles = roles,
            MustChangePassword = user.MustChangePassword,
            EmployeeId = user.EmployeeId
        });
    }


    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null || !user.IsActive)
        {
            return Unauthorized();
        }

        var result = await _userManager.ChangePasswordAsync(
            user,
            dto.CurrentPassword,
            dto.NewPassword
        );

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                errors = result.Errors.Select(e => e.Description)
            });
        }

        user.MustChangePassword = false;

        await _userManager.UpdateAsync(user);

        return Ok(new
        {
            message = "Contraseña actualizada correctamente."
        });
    }

}
