using System.ComponentModel.DataAnnotations;
using InfinityLedger.Api.Comun;
using InfinityLedger.Api.Reportes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InfinityLedger.Api.Controllers;

/// <summary>
/// Reportes contables de la empresa del usuario. Solo cuentan los asientos publicados.
/// Las fechas van en formato aaaa-mm-dd; nivelMaximo va de 1 (clase) a 4 (subcuenta).
/// </summary>
[ApiController]
[Route("api/reportes")]
[Authorize]
public class ReportesController : ControllerBase
{
    private readonly IServicioReportes _servicio;

    public ReportesController(IServicioReportes servicio)
    {
        _servicio = servicio;
    }

    /// <summary>Indicadores de la página de inicio; disponible para todos los roles.</summary>
    // Los roles de la clase y de la acción se suman en ASP.NET: por eso cada reporte declara los suyos
    // y el dashboard no tiene restricción de rol.
    [HttpGet("dashboard")]
    [ProducesResponseType<DashboardDto>(StatusCodes.Status200OK)]
    public async Task<DashboardDto> Dashboard(CancellationToken cancellationToken)
    {
        return await _servicio.DashboardAsync(Usuario.IdEmpresa, cancellationToken);
    }

    /// <summary>Saldos por cuenta auxiliar del período: guardados si está cerrado, calculados al momento si está abierto.</summary>
    [HttpGet("saldos-mensuales")]
    [Authorize(Roles = Roles.ConsultaReportes)]
    [ProducesResponseType<SaldosMensualesDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SaldosMensuales([FromQuery, Required] int idPeriodo, CancellationToken cancellationToken)
    {
        var resultado = await _servicio.SaldosMensualesAsync(Usuario.IdEmpresa, idPeriodo, cancellationToken);
        return resultado.EsExito ? Ok(resultado.Valor) : this.Error(resultado);
    }

    [HttpGet("libro-diario")]
    [Authorize(Roles = Roles.ConsultaReportes)]
    [ProducesResponseType<LibroDiarioDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> LibroDiario([FromQuery, Required] DateOnly desde, [FromQuery, Required] DateOnly hasta, CancellationToken cancellationToken)
    {
        var resultado = await _servicio.LibroDiarioAsync(Usuario.IdEmpresa, desde, hasta, cancellationToken);
        return resultado.EsExito ? Ok(resultado.Valor) : this.Error(resultado);
    }

    /// <summary>Movimientos de una cuenta con su saldo acumulado; una cuenta mayor incluye sus subcuentas.</summary>
    [HttpGet("libro-mayor")]
    [Authorize(Roles = Roles.ConsultaReportes)]
    [ProducesResponseType<LibroMayorDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LibroMayor(
        [FromQuery, Required] int idCuenta, [FromQuery, Required] DateOnly desde, [FromQuery, Required] DateOnly hasta, CancellationToken cancellationToken)
    {
        var resultado = await _servicio.LibroMayorAsync(Usuario.IdEmpresa, idCuenta, desde, hasta, cancellationToken);
        return resultado.EsExito ? Ok(resultado.Valor) : this.Error(resultado);
    }

    [HttpGet("balance-comprobacion")]
    [Authorize(Roles = Roles.ConsultaReportes)]
    [ProducesResponseType<BalanceComprobacionDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> BalanceComprobacion(
        [FromQuery, Required] DateOnly desde, [FromQuery, Required] DateOnly hasta, [FromQuery, Range(1, 4)] int nivelMaximo = 4,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _servicio.BalanceComprobacionAsync(Usuario.IdEmpresa, desde, hasta, nivelMaximo, cancellationToken);
        return resultado.EsExito ? Ok(resultado.Valor) : this.Error(resultado);
    }

    [HttpGet("estado-resultados")]
    [Authorize(Roles = Roles.ConsultaReportes)]
    [ProducesResponseType<EstadoResultadosDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> EstadoResultados(
        [FromQuery, Required] DateOnly desde, [FromQuery, Required] DateOnly hasta, [FromQuery, Range(1, 4)] int nivelMaximo = 4,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _servicio.EstadoResultadosAsync(Usuario.IdEmpresa, desde, hasta, nivelMaximo, cancellationToken);
        return resultado.EsExito ? Ok(resultado.Valor) : this.Error(resultado);
    }

    [HttpGet("balance-general")]
    [Authorize(Roles = Roles.ConsultaReportes)]
    [ProducesResponseType<BalanceGeneralDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> BalanceGeneral(
        [FromQuery, Required] DateOnly fechaCorte, [FromQuery, Range(1, 4)] int nivelMaximo = 4, CancellationToken cancellationToken = default)
    {
        var resultado = await _servicio.BalanceGeneralAsync(Usuario.IdEmpresa, fechaCorte, nivelMaximo, cancellationToken);
        return resultado.EsExito ? Ok(resultado.Valor) : this.Error(resultado);
    }

    private UsuarioActual Usuario => UsuarioActual.Desde(User, HttpContext);
}
