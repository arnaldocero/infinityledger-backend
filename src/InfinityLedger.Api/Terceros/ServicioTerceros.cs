using System.Text.RegularExpressions;
using InfinityLedger.Api.Comun;
using InfinityLedger.Api.Data;
using InfinityLedger.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace InfinityLedger.Api.Terceros;

public interface IServicioTerceros
{
    /// <param name="buscar">Texto contenido en el nombre o el número de documento.</param>
    /// <param name="tipo">CLIENTE, PROVEEDOR, EMPLEADO u OTRO.</param>
    Task<IReadOnlyList<TerceroDto>> ListarAsync(int idEmpresa, string? buscar, string? tipo, CancellationToken cancellationToken);

    Task<TerceroDto?> ObtenerAsync(int idEmpresa, int idTercero, CancellationToken cancellationToken);

    Task<Resultado<TerceroDto>> CrearAsync(UsuarioActual usuario, GuardarTerceroRequest request, CancellationToken cancellationToken);

    Task<Resultado<TerceroDto>> EditarAsync(UsuarioActual usuario, int idTercero, GuardarTerceroRequest request, CancellationToken cancellationToken);
}

/// <summary>Clientes, proveedores, empleados y otros terceros de la empresa. No hay borrado: se desactivan.</summary>
public partial class ServicioTerceros : IServicioTerceros
{
    // Mismo orden que el SET de la base: así la columna siempre se guarda igual.
    public static readonly IReadOnlyList<string> TiposTercero = ["CLIENTE", "PROVEEDOR", "EMPLEADO", "OTRO"];

    public static readonly IReadOnlySet<string> TiposDocumento = new HashSet<string> { "NIT", "CC", "CE", "PASAPORTE", "RUT" };

    private readonly CodeLedgerContext _db;

    public ServicioTerceros(CodeLedgerContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<TerceroDto>> ListarAsync(int idEmpresa, string? buscar, string? tipo, CancellationToken cancellationToken)
    {
        var consulta = _db.Terceros.AsNoTracking().Where(t => t.IdEmpresa == idEmpresa);

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var texto = buscar.Trim();
            consulta = consulta.Where(t => t.NombreRazonSocial.Contains(texto) || t.NumeroDocumento.Contains(texto));
        }

        if (!string.IsNullOrWhiteSpace(tipo))
        {
            var tipoNormalizado = tipo.Trim().ToUpperInvariant();
            consulta = consulta.Where(t => t.TipoTercero.Contains(tipoNormalizado));
        }

        var terceros = await consulta.OrderBy(t => t.NombreRazonSocial).ToListAsync(cancellationToken);
        return terceros.Select(ADto).ToList();
    }

    public async Task<TerceroDto?> ObtenerAsync(int idEmpresa, int idTercero, CancellationToken cancellationToken)
    {
        var tercero = await _db.Terceros
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.IdEmpresa == idEmpresa && t.IdTercero == idTercero, cancellationToken);

        return tercero is null ? null : ADto(tercero);
    }

    public async Task<Resultado<TerceroDto>> CrearAsync(UsuarioActual usuario, GuardarTerceroRequest request, CancellationToken cancellationToken)
    {
        var validacion = await ValidarAsync(usuario.IdEmpresa, null, request, cancellationToken);
        if (validacion.Error is not null)
        {
            return validacion.Error;
        }

        var tercero = new Tercero { IdEmpresa = usuario.IdEmpresa };
        AplicarCambios(tercero, request, validacion.Datos!);

        _db.Terceros.Add(tercero);
        Auditar(usuario, "CREAR", tercero);
        await _db.SaveChangesAsync(cancellationToken);

        return Resultado<TerceroDto>.Ok(ADto(tercero));
    }

    public async Task<Resultado<TerceroDto>> EditarAsync(UsuarioActual usuario, int idTercero, GuardarTerceroRequest request, CancellationToken cancellationToken)
    {
        var tercero = await _db.Terceros
            .FirstOrDefaultAsync(t => t.IdEmpresa == usuario.IdEmpresa && t.IdTercero == idTercero, cancellationToken);

        if (tercero is null)
        {
            return Resultado<TerceroDto>.Fallo(TipoError.NoEncontrado, "El tercero no existe.");
        }

        var validacion = await ValidarAsync(usuario.IdEmpresa, idTercero, request, cancellationToken);
        if (validacion.Error is not null)
        {
            return validacion.Error;
        }

        AplicarCambios(tercero, request, validacion.Datos!);
        Auditar(usuario, "EDITAR", tercero);
        await _db.SaveChangesAsync(cancellationToken);

        return Resultado<TerceroDto>.Ok(ADto(tercero));
    }

    /// <summary>Datos ya normalizados que se guardan tras validar.</summary>
    private sealed record DatosValidados(string NumeroDocumento, int? DigitoVerificacion, string TipoTercero);

    private sealed record Validacion(DatosValidados? Datos, Resultado<TerceroDto>? Error);

    private async Task<Validacion> ValidarAsync(int idEmpresa, int? idTercero, GuardarTerceroRequest request, CancellationToken cancellationToken)
    {
        static Validacion Invalido(string mensaje) => new(null, Resultado<TerceroDto>.Fallo(TipoError.Validacion, mensaje));

        if (!TiposDocumento.Contains(request.TipoDocumento))
        {
            return Invalido($"Tipo de documento no válido: {request.TipoDocumento}.");
        }

        var numero = request.NumeroDocumento.Trim().ToUpperInvariant();
        var esNumerico = request.TipoDocumento is "NIT" or "RUT" or "CC";

        if (esNumerico && !SoloDigitos().IsMatch(numero))
        {
            return Invalido($"El número de {request.TipoDocumento} debe tener entre 5 y 15 dígitos, sin puntos ni guiones.");
        }

        if (!esNumerico && !Alfanumerico().IsMatch(numero))
        {
            return Invalido($"El número de {request.TipoDocumento} debe tener entre 3 y 20 letras o dígitos.");
        }

        int? digito = null;
        if (request.TipoDocumento is "NIT" or "RUT")
        {
            digito = DigitoVerificacion.Calcular(numero);
            if (request.DigitoVerificacion is not null && request.DigitoVerificacion != digito)
            {
                return Invalido($"El dígito de verificación del NIT {numero} debe ser {digito}.");
            }
        }

        var tipos = request.TiposTercero.Select(t => t.Trim().ToUpperInvariant()).Distinct().ToList();
        var tipoInvalido = tipos.FirstOrDefault(t => !TiposTercero.Contains(t));
        if (tipos.Count == 0)
        {
            return Invalido("Seleccione al menos un tipo de tercero.");
        }

        if (tipoInvalido is not null)
        {
            return Invalido($"Tipo de tercero no válido: {tipoInvalido}.");
        }

        var repetido = await _db.Terceros.AnyAsync(
            t => t.IdEmpresa == idEmpresa && t.NumeroDocumento == numero && t.IdTercero != idTercero,
            cancellationToken);
        if (repetido)
        {
            return new(null, Resultado<TerceroDto>.Fallo(TipoError.Conflicto, $"Ya existe un tercero con el documento {numero}."));
        }

        var tipoTercero = string.Join(",", TiposTercero.Where(tipos.Contains));
        return new(new DatosValidados(numero, digito, tipoTercero), null);
    }

    private static void AplicarCambios(Tercero tercero, GuardarTerceroRequest request, DatosValidados datos)
    {
        tercero.TipoDocumento = request.TipoDocumento;
        tercero.NumeroDocumento = datos.NumeroDocumento;
        tercero.DigitoVerificacion = datos.DigitoVerificacion;
        tercero.NombreRazonSocial = request.NombreRazonSocial.Trim();
        tercero.TipoTercero = datos.TipoTercero;
        tercero.Direccion = Limpiar(request.Direccion);
        tercero.Telefono = Limpiar(request.Telefono);
        tercero.Email = Limpiar(request.Email)?.ToLowerInvariant();
        tercero.Estado = request.Activo;
    }

    private static string? Limpiar(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    private void Auditar(UsuarioActual usuario, string accion, Tercero tercero)
    {
        _db.AuditoriaLogs.Add(new AuditoriaLog
        {
            IdEmpresa = usuario.IdEmpresa,
            IdUsuario = usuario.IdUsuario,
            Accion = accion,
            TablaAfectada = "terceros",
            Detalle = $"{tercero.TipoDocumento} {tercero.NumeroDocumento} {tercero.NombreRazonSocial}",
            IpOrigen = usuario.Ip,
        });
    }

    private static TerceroDto ADto(Tercero t) => new(
        t.IdTercero,
        t.TipoDocumento,
        t.NumeroDocumento,
        t.DigitoVerificacion,
        t.NombreRazonSocial,
        string.IsNullOrEmpty(t.TipoTercero) ? [] : t.TipoTercero.Split(','),
        t.Direccion,
        t.Telefono,
        t.Email,
        t.Estado ?? true);

    [GeneratedRegex(@"^\d{5,15}$")]
    private static partial Regex SoloDigitos();

    [GeneratedRegex(@"^[A-Z0-9]{3,20}$")]
    private static partial Regex Alfanumerico();
}
