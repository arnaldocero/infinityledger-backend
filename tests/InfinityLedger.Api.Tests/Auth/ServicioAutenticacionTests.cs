using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using InfinityLedger.Api.Auth;
using InfinityLedger.Api.Data;
using InfinityLedger.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace InfinityLedger.Api.Tests.Auth;

public class ServicioAutenticacionTests
{
    private const string Email = "admin@codeledger.com";
    private const string Password = "clave-de-prueba";

    private static readonly DateTimeOffset Ahora = new(2026, 10, 27, 15, 0, 0, TimeSpan.Zero);

    private readonly JwtOpciones _opciones = new()
    {
        Emisor = "pruebas",
        Audiencia = "pruebas-front",
        Clave = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
        ExpiracionMinutos = 60,
    };

    [Fact]
    public async Task Login_valido_devuelve_token_con_los_datos_del_usuario()
    {
        await using var db = CrearBase();
        var servicio = CrearServicio(db);

        var respuesta = await servicio.LoginAsync(Email, Password, "127.0.0.1", CancellationToken.None);

        Assert.NotNull(respuesta);
        Assert.Equal(new UsuarioSesion(1, 1, "Administrador", "ADMIN"), respuesta.Usuario);
        Assert.Equal(Ahora.UtcDateTime.AddMinutes(60), respuesta.ExpiraEn);

        var principal = new JwtSecurityTokenHandler { MapInboundClaims = false }.ValidateToken(
            respuesta.Token, ParametrosValidacion(), out _);
        Assert.Equal("1", principal.FindFirst("sub")!.Value);
        Assert.Equal("ADMIN", principal.FindFirst(GeneradorToken.ClaimRol)!.Value);
        Assert.Equal("1", principal.FindFirst(GeneradorToken.ClaimEmpresa)!.Value);
    }

    [Fact]
    public async Task Login_valido_registra_auditoria()
    {
        await using var db = CrearBase();

        await CrearServicio(db).LoginAsync(Email, Password, "10.0.0.5", CancellationToken.None);

        var log = Assert.Single(db.AuditoriaLogs);
        Assert.Equal("LOGIN", log.Accion);
        Assert.Equal(1, log.IdUsuario);
        Assert.Equal("10.0.0.5", log.IpOrigen);
    }

    [Fact]
    public async Task Password_incorrecta_devuelve_null_y_audita_el_fallo()
    {
        await using var db = CrearBase();

        var respuesta = await CrearServicio(db).LoginAsync(Email, "otra-clave", null, CancellationToken.None);

        Assert.Null(respuesta);
        Assert.Equal("LOGIN_FALLIDO", Assert.Single(db.AuditoriaLogs).Accion);
    }

    [Fact]
    public async Task Correo_inexistente_devuelve_null_sin_auditar()
    {
        await using var db = CrearBase();

        var respuesta = await CrearServicio(db).LoginAsync("nadie@codeledger.com", Password, null, CancellationToken.None);

        Assert.Null(respuesta);
        Assert.Empty(db.AuditoriaLogs);
    }

    [Theory]
    [InlineData("INACTIVO", "ACTIVA")]
    [InlineData("ACTIVO", "INACTIVA")]
    public async Task Usuario_o_empresa_inactivos_no_pueden_entrar(string estadoUsuario, string estadoEmpresa)
    {
        await using var db = CrearBase(estadoUsuario: estadoUsuario, estadoEmpresa: estadoEmpresa);

        var respuesta = await CrearServicio(db).LoginAsync(Email, Password, null, CancellationToken.None);

        Assert.Null(respuesta);
    }

    [Fact]
    public async Task Hash_mal_formado_cuenta_como_password_incorrecta()
    {
        // Así queda el admin al cargar seed.sql, que no trae el hash real.
        await using var db = CrearBase(hash: "PENDIENTE_ASIGNAR_PASSWORD");

        var respuesta = await CrearServicio(db).LoginAsync(Email, Password, null, CancellationToken.None);

        Assert.Null(respuesta);
    }

    [Fact]
    public async Task Correo_con_espacios_alrededor_se_acepta()
    {
        await using var db = CrearBase();

        var respuesta = await CrearServicio(db).LoginAsync($"  {Email} ", Password, null, CancellationToken.None);

        Assert.NotNull(respuesta);
    }

    [Theory]
    [InlineData("")]
    [InlineData("c2hvcnQ=")] // 5 bytes
    public void Clave_jwt_ausente_o_corta_falla_al_arrancar(string clave)
    {
        Assert.Throws<InvalidOperationException>(() => GeneradorToken.CrearClave(clave));
    }

    private ServicioAutenticacion CrearServicio(CodeLedgerContext db)
    {
        var generador = new GeneradorToken(Options.Create(_opciones), new RelojFijo(Ahora));
        return new ServicioAutenticacion(db, generador);
    }

    private TokenValidationParameters ParametrosValidacion() => new()
    {
        ValidIssuer = _opciones.Emisor,
        ValidAudience = _opciones.Audiencia,
        IssuerSigningKey = GeneradorToken.CrearClave(_opciones.Clave),
        // El token se emitió con un reloj fijo; aquí solo se valida firma, emisor y audiencia.
        ValidateLifetime = false,
    };

    private static CodeLedgerContext CrearBase(
        string estadoUsuario = "ACTIVO",
        string estadoEmpresa = "ACTIVA",
        string? hash = null)
    {
        var opciones = new DbContextOptionsBuilder<CodeLedgerContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new CodeLedgerContext(opciones);

        db.Empresas.Add(new Empresa { IdEmpresa = 1, NitRut = "900000000-1", RazonSocial = "Empresa de prueba", Estado = estadoEmpresa });
        db.Usuarios.Add(new Usuario
        {
            IdUsuario = 1,
            IdEmpresa = 1,
            Nombre = "Administrador",
            Email = Email,
            // Costo 4 para que las pruebas sean rápidas
            PasswordHash = hash ?? BCrypt.Net.BCrypt.HashPassword(Password, 4),
            Rol = "ADMIN",
            Estado = estadoUsuario,
        });
        db.SaveChanges();

        return db;
    }

    private sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
    }
}
