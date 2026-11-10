
using Microsoft.IdentityModel.Tokens;
using NominaEmpresa.API.Models;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace NominaEmpresa.API.Services;

public class TokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string CreateToken(
        ApplicationUser user,
        IList<string> roles)
    {
        var key = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Falta Jwt:Key.");

        var issuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Falta Jwt:Issuer.");

        var audience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("Falta Jwt:Audience.");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? ""),
            new("mustChangePassword", user.MustChangePassword.ToString())
        };

        if (user.EmployeeId.HasValue)
        {
            claims.Add(new Claim(
                "employeeId",
                user.EmployeeId.Value.ToString()
            ));
        }

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(key)
        );

        var credentials = new SigningCredentials(
            signingKey,
            SecurityAlgorithms.HmacSha256
        );

        var expires = DateTime.UtcNow.AddMinutes(30);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
