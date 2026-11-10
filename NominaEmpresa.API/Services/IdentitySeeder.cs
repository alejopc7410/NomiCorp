
using Microsoft.AspNetCore.Identity;
using NominaEmpresa.API.Models;
using System;
using System.Threading.Tasks;

namespace NominaEmpresa.API.Services;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole>>();

        var configuration = scope.ServiceProvider
            .GetRequiredService<IConfiguration>();

        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("IdentitySeeder");

        // 1. Crear los roles si todavía no existen.
        string[] roles = { "Administrador", "Empleado" };

        foreach (var role in roles)
        {
            if (await roleManager.RoleExistsAsync(role))
                continue;

            var result = await roleManager.CreateAsync(
                new IdentityRole(role));

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"No se pudo crear el rol {role}: " +
                    string.Join(", ",
                        result.Errors.Select(e => e.Description)));
            }
        }

        // 2. Obtener el correo del administrador inicial.
        var email = configuration["InitialAdmin:Email"]?.Trim();

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException(
                "Falta configurar InitialAdmin:Email.");
        }

        // 3. Comprobar si el usuario ya existe.
        var admin = await userManager.FindByEmailAsync(email);

        if (admin != null)
        {
            // Nunca convertir automáticamente una cuenta existente
            // en administrador.
            if (!await userManager.IsInRoleAsync(
                    admin, "Administrador"))
            {
                throw new InvalidOperationException(
                    "El correo del administrador inicial pertenece " +
                    "a una cuenta existente sin el rol Administrador.");
            }

            logger.LogInformation(
                "El administrador inicial ya existe.");

            return;
        }

        // 4. Solo pedir contraseña si hay que crear la cuenta.
        var password = configuration["InitialAdmin:Password"];

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Falta configurar InitialAdmin:Password.");
        }

        // 5. Crear usuario administrador.
        admin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            MustChangePassword = true,
            IsActive = true
        };

        var createResult = await userManager.CreateAsync(
            admin, password);

        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                "No se pudo crear el administrador: " +
                string.Join(", ",
                    createResult.Errors.Select(e => e.Description)));
        }

        // 6. Asignar el rol únicamente al usuario recién creado.
        var roleResult = await userManager.AddToRoleAsync(
            admin, "Administrador");

        if (!roleResult.Succeeded)
        {
            // Revertir la creación si falla la asignación del rol.
            var deleteResult = await userManager.DeleteAsync(admin);

            if (!deleteResult.Succeeded)
            {
                logger.LogError(
                    "No se pudo revertir la creación del usuario " +
                    "después de fallar la asignación del rol.");
            }

            throw new InvalidOperationException(
                "No se pudo asignar el rol Administrador: " +
                string.Join(", ",
                    roleResult.Errors.Select(e => e.Description)));
        }

        logger.LogInformation(
            "Administrador inicial creado correctamente.");
    }
}
