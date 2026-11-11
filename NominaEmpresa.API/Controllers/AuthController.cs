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
    private readonly ILogger<AuthController> _logger;

    public AuthController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    TokenService tokenService,
    ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _logger = logger;
    }


    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);

        if (user == null || !user.IsActive)
        {
            _logger.LogWarning(
                "Login rechazado: usuario inexistente o inactivo.");

            return Unauthorized(new
            {
                message = "Correo o contraseña incorrectos."
            });
        }

        var passwordResult = await _signInManager
            .CheckPasswordSignInAsync(
                user,
                dto.Password,
                lockoutOnFailure: true
            );

        if (!passwordResult.Succeeded)
        {
            _logger.LogWarning(
                "Login rechazado. LockedOut={LockedOut}, " +
                "NotAllowed={NotAllowed}, Failed={Failed}",
                passwordResult.IsLockedOut,
                passwordResult.IsNotAllowed,
                !passwordResult.IsLockedOut &&
                !passwordResult.IsNotAllowed
            );

            return Unauthorized(new
            {
                message = "Correo o contraseña incorrectos."
            });
        }

        var roles = await _userManager.GetRolesAsync(user);

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
