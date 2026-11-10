using System.Net;
using System.Net.Http.Json;
using InfinityLedger.Api.Asientos;
using InfinityLedger.Api.Tests.Infraestructura;

namespace InfinityLedger.Api.Tests.Asientos;

public class AsientosEndpointTests : IClassFixture<ApiDePrueba>
{
    private readonly ApiDePrueba _api;

    public AsientosEndpointTests(ApiDePrueba api)
    {
        _api = api;
    }

    [Fact]
    public async Task Auditor_consulta_pero_no_registra()
    {
        var cliente = await _api.ClienteComoAsync(DatosDePrueba.AuditorEmpresa1);

        var lista = await cliente.GetAsync("/api/asientos");
        var crear = await cliente.PostAsJsonAsync("/api/asientos", AsientoCuadrado());

        Assert.Equal(HttpStatusCode.OK, lista.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, crear.StatusCode);
    }

    [Fact]
    public async Task Auxiliar_registra_un_asiento_publicado_pero_no_lo_anula()
    {
        var cliente = await _api.ClienteComoAsync(DatosDePrueba.AuxiliarEmpresa1);

        var crear = await cliente.PostAsJsonAsync("/api/asientos", AsientoCuadrado());
        var creado = await crear.Content.ReadFromJsonAsync<AsientoDto>();
        var anular = await cliente.PostAsync($"/api/asientos/{creado!.IdAsiento}/anular", null);

        Assert.Equal(HttpStatusCode.Created, crear.StatusCode);
        Assert.Equal("PUBLICADO", creado.Estado);
        Assert.Equal(HttpStatusCode.Forbidden, anular.StatusCode);
    }

    [Fact]
    public async Task Contador_anula_un_asiento()
    {
        var cliente = await _api.ClienteComoAsync(DatosDePrueba.ContadorEmpresa1);
        var creado = await (await cliente.PostAsJsonAsync("/api/asientos", AsientoCuadrado())).Content.ReadFromJsonAsync<AsientoDto>();

        var anular = await cliente.PostAsync($"/api/asientos/{creado!.IdAsiento}/anular", null);

        Assert.Equal("ANULADO", (await anular.Content.ReadFromJsonAsync<AsientoDto>())!.Estado);
    }

    [Fact]
    public async Task Asiento_descuadrado_devuelve_400_con_mensaje()
    {
        var cliente = await _api.ClienteComoAsync(DatosDePrueba.AuxiliarEmpresa1);

        var respuesta = await cliente.PostAsJsonAsync("/api/asientos", AsientoCuadrado() with
        {
            Lineas = [new LineaAsientoRequest { IdCuenta = DatosDePrueba.CajaGeneral, Debe = 10 },
                      new LineaAsientoRequest { IdCuenta = DatosDePrueba.CajaGeneral, Haber = 9 }],
        });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Contains("no cuadra", await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Asiento_de_otra_empresa_devuelve_404()
    {
        var cliente = await _api.ClienteComoAsync(DatosDePrueba.AdminEmpresa2);

        var respuesta = await cliente.GetAsync("/api/asientos/1");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task Filtro_por_periodo_en_la_url()
    {
        var cliente = await _api.ClienteComoAsync(DatosDePrueba.AuditorEmpresa1);

        var septiembre = await cliente.GetFromJsonAsync<List<AsientoResumenDto>>($"/api/asientos?idPeriodo={DatosDePrueba.Septiembre2026}");

        Assert.Equal("ING-0004", Assert.Single(septiembre!).Comprobante);
    }

    private static GuardarAsientoRequest AsientoCuadrado() => new()
    {
        IdTipoComprobante = DatosDePrueba.TipoNota,
        FechaAsiento = new DateOnly(2026, 10, 20),
        ConceptoGeneral = "Traslado entre cajas",
        Publicar = true,
        Lineas =
        [
            new LineaAsientoRequest { IdCuenta = DatosDePrueba.CajaGeneral, Debe = 500 },
            new LineaAsientoRequest { IdCuenta = DatosDePrueba.Servicios, Haber = 500, IdCentroCosto = DatosDePrueba.CentroAdministracion },
        ],
    };
}
