using System.Net;
using InfinityLedger.Api.Tests.Infraestructura;

namespace InfinityLedger.Api.Tests.Reportes;

public class ReportesEndpointTests : IClassFixture<ApiDePrueba>
{
    private readonly ApiDePrueba _api;

    public ReportesEndpointTests(ApiDePrueba api)
    {
        _api = api;
    }

    [Theory]
    [InlineData("/api/reportes/balance-comprobacion?desde=2026-10-01&hasta=2026-10-31")]
    [InlineData("/api/reportes/estado-resultados?desde=2026-10-01&hasta=2026-10-31")]
    [InlineData("/api/reportes/balance-general?fechaCorte=2026-10-31")]
    [InlineData("/api/reportes/libro-diario?desde=2026-10-01&hasta=2026-10-31")]
    [InlineData("/api/reportes/libro-mayor?idCuenta=4&desde=2026-10-01&hasta=2026-10-31")]
    [InlineData("/api/reportes/saldos-mensuales?idPeriodo=3")]
    public async Task El_auditor_consulta_los_reportes_y_el_auxiliar_no(string url)
    {
        var auditor = await (await _api.ClienteComoAsync(DatosDePrueba.AuditorEmpresa1)).GetAsync(url);
        var auxiliar = await (await _api.ClienteComoAsync(DatosDePrueba.AuxiliarEmpresa1)).GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, auditor.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, auxiliar.StatusCode);
    }

    [Fact]
    public async Task El_dashboard_lo_ven_todos_los_roles()
    {
        var respuesta = await (await _api.ClienteComoAsync(DatosDePrueba.AuxiliarEmpresa1)).GetAsync("/api/reportes/dashboard");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Theory]
    [InlineData("/api/reportes/balance-comprobacion?desde=2026-10-31&hasta=2026-10-01", "posterior")]
    [InlineData("/api/reportes/balance-comprobacion?desde=2026-10-01&hasta=2026-10-31&nivelMaximo=7", "")]
    [InlineData("/api/reportes/balance-comprobacion?hasta=2026-10-31", "")]
    public async Task Parametros_invalidos_devuelven_400(string url, string mensaje)
    {
        var respuesta = await (await _api.ClienteComoAsync(DatosDePrueba.ContadorEmpresa1)).GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Contains(mensaje, await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Saldos_de_un_periodo_de_otra_empresa_devuelve_404()
    {
        var respuesta = await (await _api.ClienteComoAsync(DatosDePrueba.AdminEmpresa1)).GetAsync(
            $"/api/reportes/saldos-mensuales?idPeriodo={DatosDePrueba.Octubre2026Empresa2}");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }
}
