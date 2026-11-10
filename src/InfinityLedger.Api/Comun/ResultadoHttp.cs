using Microsoft.AspNetCore.Mvc;

namespace InfinityLedger.Api.Comun;

public static class ResultadoHttp
{
    /// <summary>Respuesta HTTP de un resultado fallido, con cuerpo { message } que el frontend muestra tal cual.</summary>
    public static IActionResult Error<T>(this ControllerBase controller, Resultado<T> resultado)
    {
        var cuerpo = new { message = resultado.Mensaje };
        return resultado.Error switch
        {
            TipoError.NoEncontrado => controller.NotFound(cuerpo),
            TipoError.Conflicto => controller.Conflict(cuerpo),
            _ => controller.BadRequest(cuerpo),
        };
    }
}
