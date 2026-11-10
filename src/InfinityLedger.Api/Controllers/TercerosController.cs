using InfinityLedger.Api.Comun;
using InfinityLedger.Api.Terceros;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InfinityLedger.Api.Controllers;

/// <summary>Terceros de la empresa del usuario. Todos los roles consultan; el AUDITOR no modifica.</summary>
[ApiController]
[Route("api/terceros")]
[Authorize]
public class TercerosController : ControllerBase
{
    private readonly IServicioTerceros _servicio;

    public TercerosController(IServicioTerceros servicio)
    {
        _servicio = servicio;
    }

    /// <param name="buscar">Texto en el nombre o el número de documento.</param>
    /// <param name="tipo">CLIENTE, PROVEEDOR, EMPLEADO u OTRO.</param>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TerceroDto>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<TerceroDto>> Listar([FromQuery] string? buscar, [FromQuery] string? tipo, CancellationToken cancellationToken)
    {
        return await _servicio.ListarAsync(Usuario.IdEmpresa, buscar, tipo, cancellationToken);
    }

    [HttpGet("{idTercero:int}")]
    [ProducesResponseType<TerceroDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TerceroDto>> Obtener(int idTercero, CancellationToken cancellationToken)
    {
        var tercero = await _servicio.ObtenerAsync(Usuario.IdEmpresa, idTercero, cancellationToken);
        return tercero is null ? NotFound(new { message = "El tercero no existe." }) : tercero;
    }

    [HttpPost]
    [Authorize(Roles = Roles.AdminContadorAuxiliar)]
    [ProducesResponseType<TerceroDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear(GuardarTerceroRequest request, CancellationToken cancellationToken)
    {
        var resultado = await _servicio.CrearAsync(Usuario, request, cancellationToken);
        return resultado.EsExito
            ? CreatedAtAction(nameof(Obtener), new { idTercero = resultado.Valor!.IdTercero }, resultado.Valor)
            : this.Error(resultado);
    }

    [HttpPut("{idTercero:int}")]
    [Authorize(Roles = Roles.AdminContadorAuxiliar)]
    [ProducesResponseType<TerceroDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Editar(int idTercero, GuardarTerceroRequest request, CancellationToken cancellationToken)
    {
        var resultado = await _servicio.EditarAsync(Usuario, idTercero, request, cancellationToken);
        return resultado.EsExito ? Ok(resultado.Valor) : this.Error(resultado);
    }

    private UsuarioActual Usuario => UsuarioActual.Desde(User, HttpContext);
}
