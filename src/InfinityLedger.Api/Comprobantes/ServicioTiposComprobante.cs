using InfinityLedger.Api.Comun;
using InfinityLedger.Api.Data;
using InfinityLedger.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace InfinityLedger.Api.Comprobantes;

public interface IServicioTiposComprobante
{
    Task<IReadOnlyList<TipoComprobanteDto>> ListarAsync(int idEmpresa, CancellationToken cancellationToken);

    Task<Resultado<TipoComprobanteDto>> CrearAsync(UsuarioActual usuario, CrearTipoComprobanteRequest request, CancellationToken cancellationToken);

    Task<Resultado<TipoComprobanteDto>> EditarAsync(UsuarioActual usuario, int idTipoComprobante, EditarTipoComprobanteRequest request, CancellationToken cancellationToken);
}

/// <summary>Tipos de comprobante (ING, EGR, NOT...) y su consecutivo. No hay borrado.</summary>
public class ServicioTiposComprobante : IServicioTiposComprobante
{
    private readonly CodeLedgerContext _db;

    public ServicioTiposComprobante(CodeLedgerContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<TipoComprobanteDto>> ListarAsync(int idEmpresa, CancellationToken cancellationToken)
    {
        var tipos = await _db.ComprobantesTipos
            .AsNoTracking()
            .Where(t => t.IdEmpresa == idEmpresa)
            .OrderBy(t => t.Codigo)
            .ToListAsync(cancellationToken);

        return tipos.Select(ADto).ToList();
    }

    public async Task<Resultado<TipoComprobanteDto>> CrearAsync(UsuarioActual usuario, CrearTipoComprobanteRequest request, CancellationToken cancellationToken)
    {
        var codigo = request.Codigo.Trim().ToUpperInvariant();

        if (await _db.ComprobantesTipos.AnyAsync(t => t.IdEmpresa == usuario.IdEmpresa && t.Codigo == codigo, cancellationToken))
        {
            return Resultado<TipoComprobanteDto>.Fallo(TipoError.Conflicto, $"Ya existe el tipo de comprobante {codigo}.");
        }

        var tipo = new TipoComprobante
        {
            IdEmpresa = usuario.IdEmpresa,
            Codigo = codigo,
            Nombre = request.Nombre.Trim(),
            ConsecutivoActual = request.ConsecutivoActual,
        };

        _db.ComprobantesTipos.Add(tipo);
        Auditar(usuario, "CREAR", tipo);
        await _db.SaveChangesAsync(cancellationToken);

        return Resultado<TipoComprobanteDto>.Ok(ADto(tipo));
    }

    public async Task<Resultado<TipoComprobanteDto>> EditarAsync(UsuarioActual usuario, int idTipoComprobante, EditarTipoComprobanteRequest request, CancellationToken cancellationToken)
    {
        var tipo = await _db.ComprobantesTipos
            .FirstOrDefaultAsync(t => t.IdEmpresa == usuario.IdEmpresa && t.IdTipoComprobante == idTipoComprobante, cancellationToken);

        if (tipo is null)
        {
            return Resultado<TipoComprobanteDto>.Fallo(TipoError.NoEncontrado, "El tipo de comprobante no existe.");
        }

        // Bajar el consecutivo por debajo de lo ya usado repetiría números de comprobante.
        var ultimoUsado = await _db.AsientosEncabezados
            .Where(a => a.IdTipoComprobante == tipo.IdTipoComprobante)
            .MaxAsync(a => (int?)a.NumeroComprobante, cancellationToken);

        if (ultimoUsado is not null && request.ConsecutivoActual <= ultimoUsado)
        {
            return Resultado<TipoComprobanteDto>.Fallo(TipoError.Validacion,
                $"El consecutivo debe ser mayor que {ultimoUsado}: ese número ya está usado por un comprobante {tipo.Codigo}.");
        }

        tipo.Nombre = request.Nombre.Trim();
        tipo.ConsecutivoActual = request.ConsecutivoActual;
        Auditar(usuario, "EDITAR", tipo);
        await _db.SaveChangesAsync(cancellationToken);

        return Resultado<TipoComprobanteDto>.Ok(ADto(tipo));
    }

    private void Auditar(UsuarioActual usuario, string accion, TipoComprobante tipo)
    {
        _db.AuditoriaLogs.Add(new AuditoriaLog
        {
            IdEmpresa = usuario.IdEmpresa,
            IdUsuario = usuario.IdUsuario,
            Accion = accion,
            TablaAfectada = "comprobantes_tipos",
            Detalle = $"{tipo.Codigo} {tipo.Nombre} (consecutivo {tipo.ConsecutivoActual})",
            IpOrigen = usuario.Ip,
        });
    }

    private static TipoComprobanteDto ADto(TipoComprobante t) => new(t.IdTipoComprobante, t.Codigo, t.Nombre, t.ConsecutivoActual ?? 1);
}
