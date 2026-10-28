using InfinityLedger.Api.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace InfinityLedger.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IServicioAutenticacion _servicio;

    public AuthController(IServicioAutenticacion servicio)
    {
        _servicio = servicio;
    }

    /// <summary>Inicia sesión con correo y contraseña y devuelve un JWT.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(PoliticasLimite.Login)]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var respuesta = await _servicio.LoginAsync(request.Email, request.Password, ip, cancellationToken);

        // Mismo mensaje para correo inexistente, contraseña incorrecta o usuario inactivo.
        return respuesta is null
            ? Unauthorized(new { message = "Correo o contraseña incorrectos." })
            : Ok(respuesta);
    }

    /// <summary>Datos del usuario del token; sirve para comprobar que la sesión sigue vigente.</summary>
    [HttpGet("yo")]
    [Authorize]
    [ProducesResponseType<UsuarioSesion>(StatusCodes.Status200OK)]
    public ActionResult<UsuarioSesion> Yo()
    {
        return new UsuarioSesion(
            int.Parse(User.FindFirst("sub")!.Value),
            int.Parse(User.FindFirst(GeneradorToken.ClaimEmpresa)!.Value),
            User.FindFirst(GeneradorToken.ClaimNombre)!.Value,
            User.FindFirst(GeneradorToken.ClaimRol)!.Value);
    }
}
