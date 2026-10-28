using System.Threading.RateLimiting;
using InfinityLedger.Api.Auth;
using InfinityLedger.Api.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Base de datos: MariaDB 10.4 (XAMPP), base codeledger
var connectionString = builder.Configuration.GetConnectionString("CodeLedger")
    ?? throw new InvalidOperationException("Falta ConnectionStrings:CodeLedger en appsettings.");

builder.Services.AddDbContext<CodeLedgerContext>(options =>
    options.UseMySql(connectionString, new MariaDbServerVersion(new Version(10, 4, 32))));

// Autenticación con JWT
var jwt = builder.Configuration.GetSection(JwtOpciones.Seccion).Get<JwtOpciones>() ?? new JwtOpciones();
builder.Services.Configure<JwtOpciones>(builder.Configuration.GetSection(JwtOpciones.Seccion));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IGeneradorToken, GeneradorToken>();
builder.Services.AddScoped<IServicioAutenticacion, ServicioAutenticacion>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Emisor,
            ValidateAudience = true,
            ValidAudience = jwt.Audiencia,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = GeneradorToken.CrearClave(jwt.Clave),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = GeneradorToken.ClaimNombre,
            RoleClaimType = GeneradorToken.ClaimRol,
        };
    });
builder.Services.AddAuthorization();

// Límite de intentos de login por IP
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(PoliticasLimite.Login, context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
});

// CORS para el frontend Angular en desarrollo
const string CorsFrontend = "frontend";
builder.Services.AddCors(options =>
    options.AddPolicy(CorsFrontend, policy => policy
        .WithOrigins(builder.Configuration.GetSection("Cors:Origenes").Get<string[]>() ?? [])
        .AllowAnyHeader()
        .AllowAnyMethod()));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Botón "Authorize" en Swagger para probar endpoints protegidos con el token del login
    var esquema = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
    };
    options.AddSecurityDefinition("Bearer", esquema);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { [esquema] = Array.Empty<string>() });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // En desarrollo el frontend usa http://localhost:5000 para no depender del certificado de desarrollo.
    app.UseHttpsRedirection();
}

app.UseCors(CorsFrontend);

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Permite referenciar Program desde el proyecto de pruebas.
public partial class Program;
