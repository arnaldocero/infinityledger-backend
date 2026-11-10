using InfinityLedger.Api.CentrosCosto;
using InfinityLedger.Api.Comun;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InfinityLedger.Api.Controllers;

/// <summary>Centros de costo de la empresa del usuario. Todos los roles consultan; solo ADMIN y CONTADOR modifican.</summary>
[ApiController]
[Route("api/centros-costo")]
[Authorize]
public class CentrosCostoController : ControllerBase
{
    private readonly IServicioCentrosCosto _servicio;

    public CentrosCostoController(IServicioCentrosCosto servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CentroCostoDto>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<CentroCostoDto>> Listar(CancellationToken cancellationToken)
    {
        return await _servicio.ListarAsync(Usuario.IdEmpresa, cancellationToken);
    }

    [HttpPost]
    [Authorize(Roles = Roles.AdminOContador)]
    [ProducesResponseType<CentroCostoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear(GuardarCentroCostoRequest request, CancellationToken cancellationToken)
    {
        var resultado = await _servicio.CrearAsync(Usuario, request, cancellationToken);
        return resultado.EsExito
            ? StatusCode(StatusCodes.Status201Created, resultado.Valor)
            : this.Error(resultado);
    }

    [HttpPut("{idCentroCosto:int}")]
    [Authorize(Roles = Roles.AdminOContador)]
    [ProducesResponseType<CentroCostoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Editar(int idCentroCosto, GuardarCentroCostoRequest request, CancellationToken cancellationToken)
    {
        var resultado = await _servicio.EditarAsync(Usuario, idCentroCosto, request, cancellationToken);
        return resultado.EsExito ? Ok(resultado.Valor) : this.Error(resultado);
    }

    private UsuarioActual Usuario => UsuarioActual.Desde(User, HttpContext);
}
