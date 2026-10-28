using System.Security.Claims;
using InfinityLedger.Api.Auth;

namespace InfinityLedger.Api.Comun;

/// <summary>Quién hace la petición, tomado del JWT. Todas las consultas se filtran por IdEmpresa.</summary>
public record UsuarioActual(int IdUsuario, int IdEmpresa, string? Ip)
{
    public static UsuarioActual Desde(ClaimsPrincipal usuario, HttpContext http) => new(
        int.Parse(usuario.FindFirst("sub")!.Value),
        int.Parse(usuario.FindFirst(GeneradorToken.ClaimEmpresa)!.Value),
        http.Connection.RemoteIpAddress?.ToString());
}
