using System.Net;
using System.Net.Http.Json;
using InfinityLedger.Api.PlanCuentas;
using InfinityLedger.Api.Tests.Infraestructura;

namespace InfinityLedger.Api.Tests.PlanCuentas;

public class PlanCuentasEndpointTests : IClassFixture<ApiDePrueba>
{
    private readonly ApiDePrueba _api;

    public PlanCuentasEndpointTests(ApiDePrueba api)
    {
        _api = api;
    }

    [Fact]
    public async Task Sin_token_devuelve_401()
    {
        var respuesta = await _api.CreateClient().GetAsync("/api/plan-cuentas");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Cada_empresa_ve_solo_sus_cuentas()
    {
        var empresa1 = await (await _api.ClienteComoAsync(DatosDePrueba.AuxiliarEmpresa1))
            .GetFromJsonAsync<List<CuentaDto>>("/api/plan-cuentas");
        var empresa2 = await (await _api.ClienteComoAsync(DatosDePrueba.AdminEmpresa2))
            .GetFromJsonAsync<List<CuentaDto>>("/api/plan-cuentas");

        Assert.DoesNotContain(empresa1!, c => c.IdCuenta == DatosDePrueba.ActivoEmpresa2);
        Assert.Equal(DatosDePrueba.ActivoEmpresa2, Assert.Single(empresa2!).IdCuenta);
    }

    [Fact]
    public async Task Obtener_cuenta_de_otra_empresa_devuelve_404()
    {
        var cliente = await _api.ClienteComoAsync(DatosDePrueba.AdminEmpresa1);

        var respuesta = await cliente.GetAsync($"/api/plan-cuentas/{DatosDePrueba.ActivoEmpresa2}");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task Auxiliar_no_puede_crear_cuentas()
    {
        var cliente = await _api.ClienteComoAsync(DatosDePrueba.AuxiliarEmpresa1);

        var respuesta = await cliente.PostAsJsonAsync("/api/plan-cuentas", Nueva("110520"));

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task Admin_crea_cuenta_201_y_repetirla_da_409()
    {
        var cliente = await _api.ClienteComoAsync(DatosDePrueba.AdminEmpresa1);

        var creada = await cliente.PostAsJsonAsync("/api/plan-cuentas", Nueva("110515"));
        var repetida = await cliente.PostAsJsonAsync("/api/plan-cuentas", Nueva("110515"));

        Assert.Equal(HttpStatusCode.Created, creada.StatusCode);
        Assert.NotNull(creada.Headers.Location);
        var cuenta = await creada.Content.ReadFromJsonAsync<CuentaDto>();
        Assert.Equal((4, DatosDePrueba.Caja), (cuenta!.Nivel, cuenta.IdCuentaPadre));

        Assert.Equal(HttpStatusCode.Conflict, repetida.StatusCode);
        Assert.Contains("Ya existe la cuenta 110515", await repetida.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Regla_de_negocio_incumplida_devuelve_400_con_mensaje()
    {
        var cliente = await _api.ClienteComoAsync(DatosDePrueba.AdminEmpresa1);

        var respuesta = await cliente.PostAsJsonAsync("/api/plan-cuentas", Nueva("140505"));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Contains("Primero cree la cuenta 1405", await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Editar_cuenta_de_otra_empresa_devuelve_404()
    {
        var cliente = await _api.ClienteComoAsync(DatosDePrueba.AdminEmpresa1);

        var respuesta = await cliente.PutAsJsonAsync($"/api/plan-cuentas/{DatosDePrueba.ActivoEmpresa2}", new EditarCuentaRequest
        {
            NombreCuenta = "Intento",
            TipoCuenta = "ACTIVO",
            Naturaleza = "DEBITO",
        });

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    private static CrearCuentaRequest Nueva(string codigo) => new()
    {
        Codigo = codigo,
        NombreCuenta = "Cuenta de prueba",
        TipoCuenta = "ACTIVO",
        Naturaleza = "DEBITO",
        EsMovimiento = true,
    };
}
