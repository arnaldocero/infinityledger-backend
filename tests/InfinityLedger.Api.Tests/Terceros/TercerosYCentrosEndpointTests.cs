using System.Net;
using System.Net.Http.Json;
using InfinityLedger.Api.CentrosCosto;
using InfinityLedger.Api.Terceros;
using InfinityLedger.Api.Tests.Infraestructura;

namespace InfinityLedger.Api.Tests.Terceros;

public class TercerosYCentrosEndpointTests : IClassFixture<ApiDePrueba>
{
    private readonly ApiDePrueba _api;

    public TercerosYCentrosEndpointTests(ApiDePrueba api)
    {
        _api = api;
    }

    [Fact]
    public async Task Cada_empresa_ve_solo_sus_terceros()
    {
        var cliente = await _api.ClienteComoAsync(DatosDePrueba.AuditorEmpresa1);

        var terceros = await cliente.GetFromJsonAsync<List<TerceroDto>>("/api/terceros");
        var deOtraEmpresa = await cliente.GetAsync($"/api/terceros/{DatosDePrueba.TerceroEmpresa2}");

        Assert.DoesNotContain(terceros!, t => t.IdTercero == DatosDePrueba.TerceroEmpresa2);
        Assert.Equal(HttpStatusCode.NotFound, deOtraEmpresa.StatusCode);
    }

    [Fact]
    public async Task Filtro_por_tipo_en_la_url()
    {
        var cliente = await _api.ClienteComoAsync(DatosDePrueba.AuditorEmpresa1);

        var proveedores = await cliente.GetFromJsonAsync<List<TerceroDto>>("/api/terceros?tipo=PROVEEDOR");

        Assert.Equal(DatosDePrueba.ProveedorDemo, Assert.Single(proveedores!).IdTercero);
    }

    [Fact]
    public async Task Auditor_no_puede_crear_terceros()
    {
        var cliente = await _api.ClienteComoAsync(DatosDePrueba.AuditorEmpresa1);

        var respuesta = await cliente.PostAsJsonAsync("/api/terceros", NuevoTercero("1020304050"));

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task Auxiliar_crea_tercero_201_y_repetirlo_da_409()
    {
        var cliente = await _api.ClienteComoAsync(DatosDePrueba.AuxiliarEmpresa1);

        var creado = await cliente.PostAsJsonAsync("/api/terceros", NuevoTercero("1098765432"));
        var repetido = await cliente.PostAsJsonAsync("/api/terceros", NuevoTercero("1098765432"));

        Assert.Equal(HttpStatusCode.Created, creado.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, repetido.StatusCode);
        Assert.Contains("Ya existe un tercero", await repetido.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Email_invalido_devuelve_400()
    {
        var cliente = await _api.ClienteComoAsync(DatosDePrueba.AdminEmpresa1);

        var respuesta = await cliente.PostAsJsonAsync("/api/terceros", NuevoTercero("1011121314") with { Email = "no-es-correo" });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task Auxiliar_consulta_centros_pero_no_los_crea()
    {
        var cliente = await _api.ClienteComoAsync(DatosDePrueba.AuxiliarEmpresa1);

        var centros = await cliente.GetFromJsonAsync<List<CentroCostoDto>>("/api/centros-costo");
        var crear = await cliente.PostAsJsonAsync("/api/centros-costo", new GuardarCentroCostoRequest { Codigo = "CC-300", Nombre = "Ventas" });

        Assert.Equal("CC-100", Assert.Single(centros!).Codigo);
        Assert.Equal(HttpStatusCode.Forbidden, crear.StatusCode);
    }

    [Fact]
    public async Task Admin_crea_centro_en_mayusculas_y_el_codigo_repetido_da_409()
    {
        var cliente = await _api.ClienteComoAsync(DatosDePrueba.AdminEmpresa1);

        var creado = await cliente.PostAsJsonAsync("/api/centros-costo", new GuardarCentroCostoRequest { Codigo = "cc-200", Nombre = "Producción" });
        var repetido = await cliente.PostAsJsonAsync("/api/centros-costo", new GuardarCentroCostoRequest { Codigo = "CC-200", Nombre = "Otro" });

        Assert.Equal(HttpStatusCode.Created, creado.StatusCode);
        Assert.Equal("CC-200", (await creado.Content.ReadFromJsonAsync<CentroCostoDto>())!.Codigo);
        Assert.Equal(HttpStatusCode.Conflict, repetido.StatusCode);
    }

    [Fact]
    public async Task Codigo_de_centro_con_espacios_devuelve_400()
    {
        var cliente = await _api.ClienteComoAsync(DatosDePrueba.AdminEmpresa1);

        var respuesta = await cliente.PostAsJsonAsync("/api/centros-costo", new GuardarCentroCostoRequest { Codigo = "CC 400", Nombre = "Logística" });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task Editar_centro_de_otra_empresa_devuelve_404()
    {
        var cliente = await _api.ClienteComoAsync(DatosDePrueba.AdminEmpresa2);

        var respuesta = await cliente.PutAsJsonAsync($"/api/centros-costo/{DatosDePrueba.CentroAdministracion}",
            new GuardarCentroCostoRequest { Codigo = "CC-100", Nombre = "Intento" });

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    private static GuardarTerceroRequest NuevoTercero(string cedula) => new()
    {
        TipoDocumento = "CC",
        NumeroDocumento = cedula,
        NombreRazonSocial = "Persona de prueba",
        TiposTercero = ["CLIENTE"],
    };
}
