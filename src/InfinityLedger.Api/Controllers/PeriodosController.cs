using InfinityLedger.Api.Comun;
using InfinityLedger.Api.Periodos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InfinityLedger.Api.Controllers;

/// <summary>
/// Períodos fiscales de la empresa. Todos los roles consultan; ADMIN y CONTADOR crean y cierran;
/// solo ADMIN reabre un período cerrado.
/// </summary>
[ApiController]
[Route("api/periodos")]
[Authorize]
public class PeriodosController : ControllerBase
{
    private readonly IServicioPeriodos _servicio;

    public PeriodosController(IServicioPeriodos servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<PeriodoDto>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<PeriodoDto>> Listar(CancellationToken cancellationToken)
    {
        return await _servicio.ListarAsync(Usuario.IdEmpresa, cancellationToken);
    }

    [HttpPost]
    [Authorize(Roles = Roles.AdminOContador)]
    [ProducesResponseType<PeriodoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear(CrearPeriodoRequest request, CancellationToken cancellationToken)
    {
        var resultado = await _servicio.CrearAsync(Usuario, request.Anio, request.Mes, cancellationToken);
        return resultado.EsExito ? StatusCode(StatusCodes.Status201Created, resultado.Valor) : this.Error(resultado);
    }

    /// <summary>Crea los meses que falten del año; devuelve solo los creados.</summary>
    [HttpPost("anio/{anio:int:range(2000,2100)}")]
    [Authorize(Roles = Roles.AdminOContador)]
    [ProducesResponseType<IReadOnlyList<PeriodoDto>>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GenerarAnio(int anio, CancellationToken cancellationToken)
    {
        var resultado = await _servicio.GenerarAnioAsync(Usuario, anio, cancellationToken);
        return resultado.EsExito ? StatusCode(StatusCodes.Status201Created, resultado.Valor) : this.Error(resultado);
    }

    [HttpPost("{idPeriodo:int}/cerrar")]
    [Authorize(Roles = Roles.AdminOContador)]
    [ProducesResponseType<PeriodoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cerrar(int idPeriodo, CancellationToken cancellationToken)
    {
        var resultado = await _servicio.CerrarAsync(Usuario, idPeriodo, cancellationToken);
        return resultado.EsExito ? Ok(resultado.Valor) : this.Error(resultado);
    }

    [HttpPost("{idPeriodo:int}/reabrir")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType<PeriodoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reabrir(int idPeriodo, CancellationToken cancellationToken)
    {
        var resultado = await _servicio.ReabrirAsync(Usuario, idPeriodo, cancellationToken);
        return resultado.EsExito ? Ok(resultado.Valor) : this.Error(resultado);
    }

    private UsuarioActual Usuario => UsuarioActual.Desde(User, HttpContext);
}
