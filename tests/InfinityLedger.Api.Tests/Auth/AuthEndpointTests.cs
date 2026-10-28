using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using InfinityLedger.Api.Auth;
using InfinityLedger.Api.Tests.Infraestructura;

namespace InfinityLedger.Api.Tests.Auth;

/// <summary>Prueba los endpoints por HTTP con toda la canalización de ASP.NET Core y una base en memoria.</summary>
public class AuthEndpointTests : IClassFixture<ApiDePrueba>
{
    private readonly HttpClient _cliente;

    public AuthEndpointTests(ApiDePrueba api)
    {
        _cliente = api.CreateClient();
    }

    [Fact]
    public async Task Login_correcto_devuelve_200_y_el_token_sirve_para_auth_yo()
    {
        var respuesta = await _cliente.PostAsJsonAsync("/api/auth/login",
            new { email = DatosDePrueba.AdminEmpresa1, password = DatosDePrueba.Password });

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
        var respuesta = await _cliente.PostAsJsonAsync("/api/auth/login",
            new { email = DatosDePrueba.AdminEmpresa1, password = "otra" });

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Contains("incorrectos", await respuesta.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("no-es-correo", "x")]
    [InlineData("", "x")]
    [InlineData(DatosDePrueba.AdminEmpresa1, "")]
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
}
