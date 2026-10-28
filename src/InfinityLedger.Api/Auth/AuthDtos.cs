using System.ComponentModel.DataAnnotations;

namespace InfinityLedger.Api.Auth;

// Propiedades init en lugar de constructor posicional: ASP.NET Core no admite atributos de
// validación sobre las propiedades de un record posicional.
public record LoginRequest
{
    [Required, EmailAddress, MaxLength(100)]
    public string Email { get; init; } = "";

    [Required, MaxLength(200)]
    public string Password { get; init; } = "";
}

/// <summary>Mismo formato que la interfaz LoginResponse del frontend.</summary>
public record LoginResponse(string Token, DateTime ExpiraEn, UsuarioSesion Usuario);

public record UsuarioSesion(int IdUsuario, int IdEmpresa, string Nombre, string Rol);
