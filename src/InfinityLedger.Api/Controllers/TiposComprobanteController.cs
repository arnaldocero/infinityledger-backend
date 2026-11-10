using InfinityLedger.Api.Comprobantes;
using InfinityLedger.Api.Comun;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InfinityLedger.Api.Controllers;

/// <summary>Tipos de comprobante de la empresa. Todos los roles consultan; ADMIN y CONTADOR modifican.</summary>
[ApiController]
[Route("api/tipos-comprobante")]
[Authorize]
public class TiposComprobanteController : ControllerBase
{
    private readonly IServicioTiposComprobante _servicio;

    public TiposComprobanteController(IServicioTiposComprobante servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TipoComprobanteDto>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<TipoComprobanteDto>> Listar(CancellationToken cancellationToken)
    {
        return await _servicio.ListarAsync(Usuario.IdEmpresa, cancellationToken);
    }

    [HttpPost]
    [Authorize(Roles = Roles.AdminOContador)]
    [ProducesResponseType<TipoComprobanteDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear(CrearTipoComprobanteRequest request, CancellationToken cancellationToken)
    {
        var resultado = await _servicio.CrearAsync(Usuario, request, cancellationToken);
        return resultado.EsExito ? StatusCode(StatusCodes.Status201Created, resultado.Valor) : this.Error(resultado);
    }

    [HttpPut("{idTipoComprobante:int}")]
    [Authorize(Roles = Roles.AdminOContador)]
    [ProducesResponseType<TipoComprobanteDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Editar(int idTipoComprobante, EditarTipoComprobanteRequest request, CancellationToken cancellationToken)
    {
        var resultado = await _servicio.EditarAsync(Usuario, idTipoComprobante, request, cancellationToken);
        return resultado.EsExito ? Ok(resultado.Valor) : this.Error(resultado);
    }

    private UsuarioActual Usuario => UsuarioActual.Desde(User, HttpContext);
}
