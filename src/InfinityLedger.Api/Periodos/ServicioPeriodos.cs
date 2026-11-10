using InfinityLedger.Api.Comun;
using InfinityLedger.Api.Data;
using InfinityLedger.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace InfinityLedger.Api.Periodos;

public interface IServicioPeriodos
{
    Task<IReadOnlyList<PeriodoDto>> ListarAsync(int idEmpresa, CancellationToken cancellationToken);

    Task<Resultado<PeriodoDto>> CrearAsync(UsuarioActual usuario, int anio, int mes, CancellationToken cancellationToken);

    /// <summary>Crea los meses que falten del año indicado.</summary>
    Task<Resultado<IReadOnlyList<PeriodoDto>>> GenerarAnioAsync(UsuarioActual usuario, int anio, CancellationToken cancellationToken);

    Task<Resultado<PeriodoDto>> CerrarAsync(UsuarioActual usuario, int idPeriodo, CancellationToken cancellationToken);

    Task<Resultado<PeriodoDto>> ReabrirAsync(UsuarioActual usuario, int idPeriodo, CancellationToken cancellationToken);
}

/// <summary>
/// Períodos fiscales mensuales. Se cierran en orden (no puede quedar un mes anterior abierto)
/// y se reabren en orden inverso (no puede quedar un mes posterior cerrado).
/// </summary>
public class ServicioPeriodos : IServicioPeriodos
{
    public const string Abierto = "ABIERTO";
    public const string Cerrado = "CERRADO";

    private static readonly string[] Meses =
    [
        "enero", "febrero", "marzo", "abril", "mayo", "junio",
        "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre",
    ];

    private readonly CodeLedgerContext _db;

    public ServicioPeriodos(CodeLedgerContext db)
    {
        _db = db;
    }

    /// <summary>"octubre 2026".</summary>
    public static string Nombre(int anio, int mes) => $"{Meses[mes - 1]} {anio}";

    public async Task<IReadOnlyList<PeriodoDto>> ListarAsync(int idEmpresa, CancellationToken cancellationToken)
    {
        var periodos = await _db.PeriodosFiscales
            .AsNoTracking()
            .Where(p => p.IdEmpresa == idEmpresa)
            .OrderByDescending(p => p.Anio).ThenByDescending(p => p.Mes)
            .ToListAsync(cancellationToken);

        return periodos.Select(ADto).ToList();
    }

    public async Task<Resultado<PeriodoDto>> CrearAsync(UsuarioActual usuario, int anio, int mes, CancellationToken cancellationToken)
    {
        if (await _db.PeriodosFiscales.AnyAsync(p => p.IdEmpresa == usuario.IdEmpresa && p.Anio == anio && p.Mes == mes, cancellationToken))
        {
            return Resultado<PeriodoDto>.Fallo(TipoError.Conflicto, $"Ya existe el período {Nombre(anio, mes)}.");
        }

        var periodo = Nuevo(usuario.IdEmpresa, anio, mes);
        _db.PeriodosFiscales.Add(periodo);
        Auditar(usuario, "CREAR", periodo);
        await _db.SaveChangesAsync(cancellationToken);

        return Resultado<PeriodoDto>.Ok(ADto(periodo));
    }

    public async Task<Resultado<IReadOnlyList<PeriodoDto>>> GenerarAnioAsync(UsuarioActual usuario, int anio, CancellationToken cancellationToken)
    {
        var existentes = await _db.PeriodosFiscales
            .Where(p => p.IdEmpresa == usuario.IdEmpresa && p.Anio == anio)
            .Select(p => p.Mes)
            .ToListAsync(cancellationToken);

        var nuevos = Enumerable.Range(1, 12)
            .Except(existentes)
            .Select(mes => Nuevo(usuario.IdEmpresa, anio, mes))
            .ToList();

        if (nuevos.Count == 0)
        {
            return Resultado<IReadOnlyList<PeriodoDto>>.Fallo(TipoError.Conflicto, $"El año {anio} ya tiene sus 12 períodos.");
        }

        _db.PeriodosFiscales.AddRange(nuevos);
        nuevos.ForEach(p => Auditar(usuario, "CREAR", p));
        await _db.SaveChangesAsync(cancellationToken);

        return Resultado<IReadOnlyList<PeriodoDto>>.Ok(nuevos.Select(ADto).ToList());
    }

    public async Task<Resultado<PeriodoDto>> CerrarAsync(UsuarioActual usuario, int idPeriodo, CancellationToken cancellationToken)
    {
        var periodo = await BuscarAsync(usuario.IdEmpresa, idPeriodo, cancellationToken);
        if (periodo is null)
        {
            return NoEncontrado();
        }

        if (periodo.Estado == Cerrado)
        {
            return Invalido($"El período {Nombre(periodo.Anio, periodo.Mes)} ya está cerrado.");
        }

        var anteriorAbierto = await _db.PeriodosFiscales
            .Where(p => p.IdEmpresa == usuario.IdEmpresa && p.Estado != Cerrado
                && (p.Anio < periodo.Anio || (p.Anio == periodo.Anio && p.Mes < periodo.Mes)))
            .OrderBy(p => p.Anio).ThenBy(p => p.Mes)
            .FirstOrDefaultAsync(cancellationToken);

        if (anteriorAbierto is not null)
        {
            return Invalido($"Primero cierre el período {Nombre(anteriorAbierto.Anio, anteriorAbierto.Mes)}.");
        }

        var borradores = await _db.AsientosEncabezados
            .CountAsync(a => a.IdPeriodo == periodo.IdPeriodo && a.Estado == "BORRADOR", cancellationToken);

        if (borradores > 0)
        {
            return Invalido($"El período tiene {borradores} asiento(s) en borrador; publíquelos o anúlelos antes de cerrar.");
        }

        periodo.Estado = Cerrado;
        Auditar(usuario, "CERRAR", periodo);
        await _db.SaveChangesAsync(cancellationToken);

        return Resultado<PeriodoDto>.Ok(ADto(periodo));
    }

    public async Task<Resultado<PeriodoDto>> ReabrirAsync(UsuarioActual usuario, int idPeriodo, CancellationToken cancellationToken)
    {
        var periodo = await BuscarAsync(usuario.IdEmpresa, idPeriodo, cancellationToken);
        if (periodo is null)
        {
            return NoEncontrado();
        }

        if (periodo.Estado != Cerrado)
        {
            return Invalido($"El período {Nombre(periodo.Anio, periodo.Mes)} ya está abierto.");
        }

        var posteriorCerrado = await _db.PeriodosFiscales
            .Where(p => p.IdEmpresa == usuario.IdEmpresa && p.Estado == Cerrado
                && (p.Anio > periodo.Anio || (p.Anio == periodo.Anio && p.Mes > periodo.Mes)))
            .OrderByDescending(p => p.Anio).ThenByDescending(p => p.Mes)
            .FirstOrDefaultAsync(cancellationToken);

        if (posteriorCerrado is not null)
        {
            return Invalido($"Primero reabra el período {Nombre(posteriorCerrado.Anio, posteriorCerrado.Mes)}.");
        }

        periodo.Estado = Abierto;
        Auditar(usuario, "REABRIR", periodo);
        await _db.SaveChangesAsync(cancellationToken);

        return Resultado<PeriodoDto>.Ok(ADto(periodo));
    }

    private Task<PeriodoFiscal?> BuscarAsync(int idEmpresa, int idPeriodo, CancellationToken cancellationToken) =>
        _db.PeriodosFiscales.FirstOrDefaultAsync(p => p.IdEmpresa == idEmpresa && p.IdPeriodo == idPeriodo, cancellationToken);

    private static PeriodoFiscal Nuevo(int idEmpresa, int anio, int mes) => new()
    {
        IdEmpresa = idEmpresa,
        Anio = anio,
        Mes = mes,
        FechaInicio = new DateOnly(anio, mes, 1),
        FechaFin = new DateOnly(anio, mes, DateTime.DaysInMonth(anio, mes)),
        Estado = Abierto,
    };

    private void Auditar(UsuarioActual usuario, string accion, PeriodoFiscal periodo)
    {
        _db.AuditoriaLogs.Add(new AuditoriaLog
        {
            IdEmpresa = usuario.IdEmpresa,
            IdUsuario = usuario.IdUsuario,
            Accion = accion,
            TablaAfectada = "periodos_fiscales",
            Detalle = Nombre(periodo.Anio, periodo.Mes),
            IpOrigen = usuario.Ip,
        });
    }

    private static Resultado<PeriodoDto> Invalido(string mensaje) => Resultado<PeriodoDto>.Fallo(TipoError.Validacion, mensaje);

    private static Resultado<PeriodoDto> NoEncontrado() => Resultado<PeriodoDto>.Fallo(TipoError.NoEncontrado, "El período no existe.");

    private static PeriodoDto ADto(PeriodoFiscal p) => new(p.IdPeriodo, p.Anio, p.Mes, p.FechaInicio, p.FechaFin, p.Estado ?? Abierto);
}
