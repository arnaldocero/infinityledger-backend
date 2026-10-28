using InfinityLedger.Api.Comun;
using InfinityLedger.Api.PlanCuentas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InfinityLedger.Api.Controllers;

/// <summary>Plan de cuentas de la empresa del usuario. Todos los roles lo consultan; solo ADMIN y CONTADOR lo modifican.</summary>
[ApiController]
[Route("api/plan-cuentas")]
[Authorize]
public class PlanCuentasController : ControllerBase
{
    private readonly IServicioPlanCuentas _servicio;

    public PlanCuentasController(IServicioPlanCuentas servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CuentaDto>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<CuentaDto>> Listar(CancellationToken cancellationToken)
    {
        return await _servicio.ListarAsync(Usuario.IdEmpresa, cancellationToken);
    }

    [HttpGet("{idCuenta:int}")]
    [ProducesResponseType<CuentaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CuentaDto>> Obtener(int idCuenta, CancellationToken cancellationToken)
    {
        var cuenta = await _servicio.ObtenerAsync(Usuario.IdEmpresa, idCuenta, cancellationToken);
        return cuenta is null ? NotFound(new { message = "La cuenta no existe." }) : cuenta;
    }

    [HttpPost]
    [Authorize(Roles = Roles.AdminOContador)]
    [ProducesResponseType<CuentaDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear(CrearCuentaRequest request, CancellationToken cancellationToken)
    {
        var resultado = await _servicio.CrearAsync(Usuario, request, cancellationToken);
        return resultado.EsExito
            ? CreatedAtAction(nameof(Obtener), new { idCuenta = resultado.Valor!.IdCuenta }, resultado.Valor)
            : Error(resultado);
    }

    [HttpPut("{idCuenta:int}")]
    [Authorize(Roles = Roles.AdminOContador)]
    [ProducesResponseType<CuentaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Editar(int idCuenta, EditarCuentaRequest request, CancellationToken cancellationToken)
    {
        var resultado = await _servicio.EditarAsync(Usuario, idCuenta, request, cancellationToken);
        return resultado.EsExito ? Ok(resultado.Valor) : Error(resultado);
    }

    private UsuarioActual Usuario => UsuarioActual.Desde(User, HttpContext);

    private IActionResult Error<T>(Resultado<T> resultado)
    {
        var cuerpo = new { message = resultado.Mensaje };
        return resultado.Error switch
        {
            TipoError.NoEncontrado => NotFound(cuerpo),
            TipoError.Conflicto => Conflict(cuerpo),
            _ => BadRequest(cuerpo),
        };
    }
}
