
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NominaEmpresa.API.Data;
using NominaEmpresa.API.Models;
using NominaEmpresa.API.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Controladores y OpenAPI
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// 2. Base de datos
var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Falta configurar ConnectionStrings:DefaultConnection.");

builder.Services.AddDbContext<PayrollDbContext>(options =>
    options.UseSqlServer(connectionString));

// 3. Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 10;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;

    options.User.RequireUniqueEmail = true;

    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan =
        TimeSpan.FromMinutes(15);
})
.AddEntityFrameworkStores<PayrollDbContext>()
.AddDefaultTokenProviders();

// 4. JWT
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "Falta configurar Jwt:Key.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException(
        "Falta configurar Jwt:Issuer.");

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException(
        "Falta configurar Jwt:Audience.");

if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key debe contener al menos 32 bytes en UTF-8.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme =
        JwtBearerDefaults.AuthenticationScheme;

    options.DefaultChallengeScheme =
        JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata =
        !builder.Environment.IsDevelopment();

    options.TokenValidationParameters =
        new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)),

            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = true,
            ValidAudience = jwtAudience,

            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
});

// 5. Autorización
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("PasswordChangeCompleted", policy =>
    {
        policy.RequireAuthenticatedUser();

        policy.RequireAssertion(context =>
        {
            var mustChange = context.User
                .FindFirst("mustChangePassword")?.Value;

            return string.Equals(
                mustChange,
                "False",
                StringComparison.OrdinalIgnoreCase);
        });
    });
});

// 6. Servicios de NomiCorp
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<PayrollCalculationService>();
builder.Services.AddScoped<BonusCalculationService>();
builder.Services.AddScoped<BonusSettlementService>();
builder.Services.AddScoped<BonusValidationService>();
builder.Services.AddScoped<BonusPaymentReconciliationService>();
builder.Services.AddScoped<BonusLaborIncidentService>();
builder.Services.AddScoped<BonusPayrollIntegrationService>();

// 7. CORS
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?? Array.Empty<string>();

if (allowedOrigins.Length == 0)
{
    throw new InvalidOperationException(
        "Debes configurar Cors:AllowedOrigins.");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactApp", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// 8. Construcción
var app = builder.Build();

// 9. Seeder controlado
var runSeeder = builder.Configuration
    .GetValue<bool>("Database:RunSeeder");

if (runSeeder)
{
    await IdentitySeeder.SeedAsync(app.Services);
}

// 10. OpenAPI solo en desarrollo
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// 11. Middleware
app.UseRouting();

app.UseCors("ReactApp");

app.UseAuthentication();
app.UseAuthorization();

// 12. Endpoints
app.MapGet("/health", () =>
    Results.Ok(new
    {
        status = "healthy",
        application = "NomiCorp API"
    }))
    .AllowAnonymous();

app.MapControllers();

app.Run();




/*using Microsoft.EntityFrameworkCore;
using NominaEmpresa.API.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddDbContext<PayrollDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactDev", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Durante el desarrollo local por HTTP, mantenemos desactivada
// la redirección HTTPS.
app.UseCors("ReactDev");

app.MapControllers();

app.Run();*/


/*var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}*/
