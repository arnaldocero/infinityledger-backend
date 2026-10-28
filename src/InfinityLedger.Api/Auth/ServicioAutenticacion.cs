using InfinityLedger.Api.Data;
using InfinityLedger.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace InfinityLedger.Api.Auth;

public interface IServicioAutenticacion
{
    /// <summary>Devuelve null si el correo no existe, la contraseña no coincide o el usuario o su empresa están inactivos.</summary>
    Task<LoginResponse?> LoginAsync(string email, string password, string? ip, CancellationToken cancellationToken);
}

public class ServicioAutenticacion : IServicioAutenticacion
{
    private const string RolPorDefecto = "AUXILIAR";

    // Hash de relleno: cuando el correo no existe se verifica igual contra él, así la
    // respuesta tarda lo mismo y no revela qué correos están registrados.
    private static readonly Lazy<string> HashRelleno = new(() => BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString(), 11));

    private readonly CodeLedgerContext _db;
    private readonly IGeneradorToken _generadorToken;

    public ServicioAutenticacion(CodeLedgerContext db, IGeneradorToken generadorToken)
    {
        _db = db;
        _generadorToken = generadorToken;
    }

    public async Task<LoginResponse?> LoginAsync(string email, string password, string? ip, CancellationToken cancellationToken)
    {
        var emailNormalizado = email.Trim();

        var usuario = await _db.Usuarios
            .Include(u => u.IdEmpresaNavigation)
            .FirstOrDefaultAsync(u => u.Email == emailNormalizado, cancellationToken);

        if (usuario is null)
        {
            VerificarPassword(password, HashRelleno.Value);
            return null;
        }

        var valido = VerificarPassword(password, usuario.PasswordHash)
            && usuario.Estado == "ACTIVO"
            && usuario.IdEmpresaNavigation.Estado == "ACTIVA";

        Auditar(usuario, valido ? "LOGIN" : "LOGIN_FALLIDO", ip);
        await _db.SaveChangesAsync(cancellationToken);

        if (!valido)
        {
            return null;
        }

        var sesion = new UsuarioSesion(usuario.IdUsuario, usuario.IdEmpresa, usuario.Nombre, usuario.Rol ?? RolPorDefecto);
        var (token, expiraEn) = _generadorToken.Generar(sesion, usuario.Email);

        return new LoginResponse(token, expiraEn, sesion);
    }

    /// <summary>Un hash mal formado (p. ej. 'PENDIENTE_ASIGNAR_PASSWORD' del seed) cuenta como contraseña incorrecta.</summary>
    private static bool VerificarPassword(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }

    private void Auditar(Usuario usuario, string accion, string? ip)
    {
        _db.AuditoriaLogs.Add(new AuditoriaLog
        {
            IdEmpresa = usuario.IdEmpresa,
            IdUsuario = usuario.IdUsuario,
            Accion = accion,
            TablaAfectada = "usuarios",
            IpOrigen = ip,
        });
    }
}
