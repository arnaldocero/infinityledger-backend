using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace InfinityLedger.Api.Auth;

public interface IGeneradorToken
{
    (string Token, DateTime ExpiraEn) Generar(UsuarioSesion usuario, string email);
}

public class GeneradorToken : IGeneradorToken
{
    /// <summary>Nombres cortos de los claims; Program.cs valida el token con estos mismos nombres.</summary>
    public const string ClaimRol = "role";
    public const string ClaimNombre = "name";
    public const string ClaimEmpresa = "id_empresa";

    private readonly JwtOpciones _opciones;
    private readonly TimeProvider _reloj;

    public GeneradorToken(IOptions<JwtOpciones> opciones, TimeProvider reloj)
    {
        _opciones = opciones.Value;
        _reloj = reloj;
    }

    public (string Token, DateTime ExpiraEn) Generar(UsuarioSesion usuario, string email)
    {
        var ahora = _reloj.GetUtcNow().UtcDateTime;
        var expiraEn = ahora.AddMinutes(_opciones.ExpiracionMinutos);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.IdUsuario.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimNombre, usuario.Nombre),
            new Claim(ClaimRol, usuario.Rol),
            new Claim(ClaimEmpresa, usuario.IdEmpresa.ToString()),
        };

        var credenciales = new SigningCredentials(CrearClave(_opciones.Clave), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _opciones.Emisor,
            audience: _opciones.Audiencia,
            claims: claims,
            notBefore: ahora,
            expires: expiraEn,
            signingCredentials: credenciales);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiraEn);
    }

    /// <summary>Clave simétrica a partir de la configuración; falla al arrancar si falta o es corta.</summary>
    public static SymmetricSecurityKey CrearClave(string claveBase64)
    {
        if (string.IsNullOrWhiteSpace(claveBase64))
        {
            throw new InvalidOperationException(
                "Falta Jwt:Clave. En desarrollo: dotnet user-secrets set \"Jwt:Clave\" \"<clave base64>\".");
        }

        var bytes = Convert.FromBase64String(claveBase64);
        if (bytes.Length < 32)
        {
            throw new InvalidOperationException("Jwt:Clave debe tener al menos 32 bytes.");
        }

        return new SymmetricSecurityKey(bytes);
    }
}
