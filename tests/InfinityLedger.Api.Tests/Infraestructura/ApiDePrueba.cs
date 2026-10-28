using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using InfinityLedger.Api.Auth;
using InfinityLedger.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InfinityLedger.Api.Tests.Infraestructura;

/// <summary>
/// La API completa sobre una base en memoria con los datos de <see cref="DatosDePrueba"/>.
/// Cada clase de pruebas que la usa como IClassFixture recibe su propia base.
/// </summary>
public class ApiDePrueba : WebApplicationFactory<Program>
{
    private readonly string _nombreBase = Guid.NewGuid().ToString();

    // El login tiene rate limiting por IP y en TestServer todas las peticiones comparten IP,
    // así que se inicia sesión una sola vez por usuario.
    private readonly ConcurrentDictionary<string, string> _tokens = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:CodeLedger", "no-se-usa");
        builder.UseSetting("Jwt:Clave", Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)));

        builder.ConfigureServices(services =>
        {
            // Cambia MariaDB por una base en memoria
            services.RemoveAll<DbContextOptions<CodeLedgerContext>>();
            services.AddDbContext<CodeLedgerContext>(options => options.UseInMemoryDatabase(_nombreBase));

            using var scope = services.BuildServiceProvider().CreateScope();
            DatosDePrueba.Cargar(scope.ServiceProvider.GetRequiredService<CodeLedgerContext>());
        });
    }

    /// <summary>Cliente HTTP con el token del usuario indicado (ver DatosDePrueba).</summary>
    public async Task<HttpClient> ClienteComoAsync(string email)
    {
        var cliente = CreateClient();

        if (!_tokens.TryGetValue(email, out var token))
        {
            var respuesta = await cliente.PostAsJsonAsync("/api/auth/login", new { email, password = DatosDePrueba.Password });
            respuesta.EnsureSuccessStatusCode();
            token = (await respuesta.Content.ReadFromJsonAsync<LoginResponse>())!.Token;
            _tokens[email] = token;
        }

        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cliente;
    }

    /// <summary>Contexto sobre la misma base en memoria, para revisar lo que guardó la API.</summary>
    public CodeLedgerContext CrearContexto()
    {
        var opciones = new DbContextOptionsBuilder<CodeLedgerContext>().UseInMemoryDatabase(_nombreBase).Options;
        return new CodeLedgerContext(opciones);
    }
}
