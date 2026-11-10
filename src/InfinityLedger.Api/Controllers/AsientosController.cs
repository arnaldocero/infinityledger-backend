using InfinityLedger.Api.Asientos;
using InfinityLedger.Api.Comun;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InfinityLedger.Api.Controllers;

/// <summary>
/// Asientos contables de la empresa. Todos los roles consultan; ADMIN, CONTADOR y AUXILIAR registran,
/// editan borradores y publican; solo ADMIN y CONTADOR anulan.
/// </summary>
[ApiController]
[Route("api/asientos")]
[Authorize]
public class AsientosController : ControllerBase
{
    private readonly IServicioAsientos _servicio;

    public AsientosController(IServicioAsientos servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AsientoResumenDto>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<AsientoResumenDto>> Listar([FromQuery] FiltroAsientos filtro, CancellationToken cancellationToken)
    {
        return await _servicio.ListarAsync(Usuario.IdEmpresa, filtro, cancellationToken);
    }

    [HttpGet("{idAsiento:int}")]
    [ProducesResponseType<AsientoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AsientoDto>> Obtener(int idAsiento, CancellationToken cancellationToken)
    {
        var asiento = await _servicio.ObtenerAsync(Usuario.IdEmpresa, idAsiento, cancellationToken);
        return asiento is null ? NotFound(new { message = "El asiento no existe." }) : asiento;
    }

    /// <summary>Crea el asiento con el siguiente número de su tipo; queda en borrador o publicado según <c>publicar</c>.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.AdminContadorAuxiliar)]
    [ProducesResponseType<AsientoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear(GuardarAsientoRequest request, CancellationToken cancellationToken)
    {
        var resultado = await _servicio.CrearAsync(Usuario, request, cancellationToken);
        return resultado.EsExito
            ? CreatedAtAction(nameof(Obtener), new { idAsiento = resultado.Valor!.IdAsiento }, resultado.Valor)
            : this.Error(resultado);
    }

    /// <summary>Reemplaza un borrador (encabezado y líneas). Con <c>publicar</c> también lo publica.</summary>
    [HttpPut("{idAsiento:int}")]
    [Authorize(Roles = Roles.AdminContadorAuxiliar)]
    [ProducesResponseType<AsientoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Editar(int idAsiento, GuardarAsientoRequest request, CancellationToken cancellationToken)
    {
        var resultado = await _servicio.EditarAsync(Usuario, idAsiento, request, cancellationToken);
        return resultado.EsExito ? Ok(resultado.Valor) : this.Error(resultado);
    }

    [HttpPost("{idAsiento:int}/publicar")]
    [Authorize(Roles = Roles.AdminContadorAuxiliar)]
    [ProducesResponseType<AsientoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Publicar(int idAsiento, CancellationToken cancellationToken)
    {
        var resultado = await _servicio.PublicarAsync(Usuario, idAsiento, cancellationToken);
        return resultado.EsExito ? Ok(resultado.Valor) : this.Error(resultado);
    }

    [HttpPost("{idAsiento:int}/anular")]
    [Authorize(Roles = Roles.AdminOContador)]
    [ProducesResponseType<AsientoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Anular(int idAsiento, CancellationToken cancellationToken)
    {
        var resultado = await _servicio.AnularAsync(Usuario, idAsiento, cancellationToken);
        return resultado.EsExito ? Ok(resultado.Valor) : this.Error(resultado);
    }

    private UsuarioActual Usuario => UsuarioActual.Desde(User, HttpContext);
}
