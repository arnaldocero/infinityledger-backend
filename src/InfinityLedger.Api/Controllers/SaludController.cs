using InfinityLedger.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InfinityLedger.Api.Controllers;

/// <summary>Comprueba que la API está arriba y que puede consultar la base de datos.</summary>
[ApiController]
[Route("api/salud")]
public class SaludController : ControllerBase
{
    private readonly CodeLedgerContext _db;

    public SaludController(CodeLedgerContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        try
        {
            var empresas = await _db.Empresas.CountAsync(cancellationToken);
            var cuentas = await _db.PlanCuentas.CountAsync(cancellationToken);
            var periodosAbiertos = await _db.PeriodosFiscales
                .CountAsync(p => p.Estado == "ABIERTO", cancellationToken);

            return Ok(new { estado = "ok", baseDatos = "conectada", empresas, cuentas, periodosAbiertos });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { estado = "error", baseDatos = "sin conexión", detalle = ex.Message });
        }
    }
}
