using System.Net;
using System.Net.Http.Json;
using InfinityLedger.Api.Comprobantes;
using InfinityLedger.Api.Comun;
using InfinityLedger.Api.Data;
using InfinityLedger.Api.Periodos;
using InfinityLedger.Api.Tests.Infraestructura;
using Microsoft.EntityFrameworkCore;

namespace InfinityLedger.Api.Tests.Periodos;

public class ServicioTiposComprobanteTests
{
    private static readonly UsuarioActual AdminEmpresa1 = new(1, 1, null);

    [Fact]
    public async Task Crear_guarda_el_codigo_en_mayusculas_y_no_permite_repetirlo()
    {
        await using var db = CrearBase();
        var servicio = new ServicioTiposComprobante(db);

        var nuevo = await servicio.CrearAsync(AdminEmpresa1, new CrearTipoComprobanteRequest { Codigo = "rc", Nombre = "Recibo de caja" }, CancellationToken.None);
        var repetido = await servicio.CrearAsync(AdminEmpresa1, new CrearTipoComprobanteRequest { Codigo = "ING", Nombre = "Otro" }, CancellationToken.None);

        Assert.Equal(("RC", 1), (nuevo.Valor!.Codigo, nuevo.Valor.ConsecutivoActual));
        Assert.Equal(TipoError.Conflicto, repetido.Error);
    }

    [Theory]
    [InlineData(DatosDePrueba.TipoIngreso, 4, false)] // ING ya usó el 4
    [InlineData(DatosDePrueba.TipoIngreso, 5, true)]
    [InlineData(DatosDePrueba.TipoIngreso, 50, true)]
    [InlineData(DatosDePrueba.TipoNota, 1, false)]    // NOT ya usó el 1
    public async Task El_consecutivo_no_puede_bajar_a_un_numero_ya_usado(int idTipo, int consecutivo, bool valido)
    {
        await using var db = CrearBase();

        var resultado = await new ServicioTiposComprobante(db).EditarAsync(AdminEmpresa1, idTipo,
            new EditarTipoComprobanteRequest { Nombre = "Nombre editado", ConsecutivoActual = consecutivo }, CancellationToken.None);

        Assert.Equal(valido, resultado.EsExito);
        if (!valido)
        {
            Assert.Contains("ya está usado", resultado.Mensaje);
        }
    }

    [Fact]
    public async Task Editar_tipo_de_otra_empresa_no_lo_encuentra()
    {
        await using var db = CrearBase();

        var resultado = await new ServicioTiposComprobante(db).EditarAsync(AdminEmpresa1, DatosDePrueba.TipoIngresoEmpresa2,
            new EditarTipoComprobanteRequest { Nombre = "Intento", ConsecutivoActual = 9 }, CancellationToken.None);

        Assert.Equal(TipoError.NoEncontrado, resultado.Error);
    }

    private static CodeLedgerContext CrearBase()
    {
        var opciones = new DbContextOptionsBuilder<CodeLedgerContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new CodeLedgerContext(opciones);
        DatosDePrueba.Cargar(db);
        return db;
    }
}

public class TiposComprobanteYPeriodosEndpointTests : IClassFixture<ApiDePrueba>
{
    private readonly ApiDePrueba _api;

    public TiposComprobanteYPeriodosEndpointTests(ApiDePrueba api)
    {
        _api = api;
    }

    [Fact]
    public async Task Todos_los_roles_consultan_periodos_y_tipos_de_su_empresa()
    {
        var cliente = await _api.ClienteComoAsync(DatosDePrueba.AuditorEmpresa1);

        var periodos = await cliente.GetFromJsonAsync<List<PeriodoDto>>("/api/periodos");
        var tipos = await cliente.GetFromJsonAsync<List<TipoComprobanteDto>>("/api/tipos-comprobante");

        Assert.DoesNotContain(periodos!, p => p.IdPeriodo == DatosDePrueba.Octubre2026Empresa2);
        Assert.Equal(new[] { "ING", "NOT" }, tipos!.Select(t => t.Codigo));
    }

    [Fact]
    public async Task Auxiliar_no_puede_cerrar_periodos_ni_crear_tipos()
    {
        var cliente = await _api.ClienteComoAsync(DatosDePrueba.AuxiliarEmpresa1);

        var cerrar = await cliente.PostAsync($"/api/periodos/{DatosDePrueba.Septiembre2026}/cerrar", null);
        var crearTipo = await cliente.PostAsJsonAsync("/api/tipos-comprobante", new CrearTipoComprobanteRequest { Codigo = "RC", Nombre = "Recibo" });

        Assert.Equal(HttpStatusCode.Forbidden, cerrar.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, crearTipo.StatusCode);
    }

    [Fact]
    public async Task Contador_no_puede_reabrir_periodos()
    {
        var cliente = await _api.ClienteComoAsync(DatosDePrueba.ContadorEmpresa1);

        var respuesta = await cliente.PostAsync($"/api/periodos/{DatosDePrueba.Agosto2026}/reabrir", null);

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task Admin_cierra_y_reabre_un_periodo()
    {
        var cliente = await _api.ClienteComoAsync(DatosDePrueba.AdminEmpresa2);

        var cerrar = await cliente.PostAsync($"/api/periodos/{DatosDePrueba.Octubre2026Empresa2}/cerrar", null);
        var reabrir = await cliente.PostAsync($"/api/periodos/{DatosDePrueba.Octubre2026Empresa2}/reabrir", null);

        Assert.Equal("CERRADO", (await cerrar.Content.ReadFromJsonAsync<PeriodoDto>())!.Estado);
        Assert.Equal("ABIERTO", (await reabrir.Content.ReadFromJsonAsync<PeriodoDto>())!.Estado);
    }

    [Fact]
    public async Task Cerrar_fuera_de_orden_devuelve_400_con_mensaje()
    {
        var cliente = await _api.ClienteComoAsync(DatosDePrueba.ContadorEmpresa1);

        var respuesta = await cliente.PostAsync($"/api/periodos/{DatosDePrueba.Octubre2026}/cerrar", null);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Contains("septiembre 2026", await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Mes_fuera_de_rango_devuelve_400()
    {
        var cliente = await _api.ClienteComoAsync(DatosDePrueba.ContadorEmpresa1);

        var respuesta = await cliente.PostAsJsonAsync("/api/periodos", new CrearPeriodoRequest { Anio = 2026, Mes = 13 });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }
}
