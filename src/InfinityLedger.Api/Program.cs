using InfinityLedger.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Base de datos: MariaDB 10.4 (XAMPP), base codeledger
var connectionString = builder.Configuration.GetConnectionString("CodeLedger")
    ?? throw new InvalidOperationException("Falta ConnectionStrings:CodeLedger en appsettings.");

builder.Services.AddDbContext<CodeLedgerContext>(options =>
    options.UseMySql(connectionString, new MariaDbServerVersion(new Version(10, 4, 32))));

// CORS para el frontend Angular en desarrollo
const string CorsFrontend = "frontend";
builder.Services.AddCors(options =>
    options.AddPolicy(CorsFrontend, policy => policy
        .WithOrigins(builder.Configuration.GetSection("Cors:Origenes").Get<string[]>() ?? [])
        .AllowAnyHeader()
        .AllowAnyMethod()));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(CorsFrontend);

app.UseAuthorization();

app.MapControllers();

app.Run();
