using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using InfinityLedger.Api.Auth;
using InfinityLedger.Api.Data;
using InfinityLedger.Api.Data.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InfinityLedger.Api.Tests.Auth;

/// <summary>Prueba los endpoints por HTTP con toda la canalización de ASP.NET Core y una base en memoria.</summary>
public class AuthEndpointTests : IClassFixture<AuthEndpointTests.ApiDePrueba>
{
    private const string Email = "admin@codeledger.com";
    private const string Password = "clave-de-prueba";

    private readonly HttpClient _cliente;

    public AuthEndpointTests(ApiDePrueba api)
    {
        _cliente = api.CreateClient();
    }

    [Fact]
    public async Task Login_correcto_devuelve_200_y_el_token_sirve_para_auth_yo()
    {
        var respuesta = await _cliente.PostAsJsonAsync("/api/auth/login", new { email = Email, password = Password });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var login = await respuesta.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.False(string.IsNullOrEmpty(login!.Token));

        var peticion = new HttpRequestMessage(HttpMethod.Get, "/api/auth/yo");
        peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
        var yo = await _cliente.SendAsync(peticion);

        Assert.Equal(HttpStatusCode.OK, yo.StatusCode);
        Assert.Equal(login.Usuario, await yo.Content.ReadFromJsonAsync<UsuarioSesion>());
    }

    [Fact]
    public async Task Login_incorrecto_devuelve_401_con_mensaje()
    {
        var respuesta = await _cliente.PostAsJsonAsync("/api/auth/login", new { email = Email, password = "otra" });

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Contains("incorrectos", await respuesta.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("no-es-correo", "x")]
    [InlineData("", "x")]
    [InlineData(Email, "")]
    public async Task Datos_invalidos_devuelven_400(string email, string password)
    {
        var respuesta = await _cliente.PostAsJsonAsync("/api/auth/login", new { email, password });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task Auth_yo_sin_token_devuelve_401()
    {
        var respuesta = await _cliente.GetAsync("/api/auth/yo");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    public class ApiDePrueba : WebApplicationFactory<Program>
    {
        private readonly string _nombreBase = Guid.NewGuid().ToString();

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
                var db = scope.ServiceProvider.GetRequiredService<CodeLedgerContext>();
                db.Empresas.Add(new Empresa { IdEmpresa = 1, NitRut = "900000000-1", RazonSocial = "Empresa de prueba", Estado = "ACTIVA" });
                db.Usuarios.Add(new Usuario
                {
                    IdUsuario = 1,
                    IdEmpresa = 1,
                    Nombre = "Administrador",
                    Email = Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password, 4),
                    Rol = "ADMIN",
                    Estado = "ACTIVO",
                });
                db.SaveChanges();
            });
        }
    }
}
