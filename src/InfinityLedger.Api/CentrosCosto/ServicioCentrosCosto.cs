using InfinityLedger.Api.Comun;
using InfinityLedger.Api.Data;
using InfinityLedger.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace InfinityLedger.Api.CentrosCosto;

public interface IServicioCentrosCosto
{
    Task<IReadOnlyList<CentroCostoDto>> ListarAsync(int idEmpresa, CancellationToken cancellationToken);

    Task<Resultado<CentroCostoDto>> CrearAsync(UsuarioActual usuario, GuardarCentroCostoRequest request, CancellationToken cancellationToken);

    Task<Resultado<CentroCostoDto>> EditarAsync(UsuarioActual usuario, int idCentroCosto, GuardarCentroCostoRequest request, CancellationToken cancellationToken);
}

/// <summary>Centros de costo de la empresa. No hay borrado: se desactivan.</summary>
public class ServicioCentrosCosto : IServicioCentrosCosto
{
    private readonly CodeLedgerContext _db;

    public ServicioCentrosCosto(CodeLedgerContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CentroCostoDto>> ListarAsync(int idEmpresa, CancellationToken cancellationToken)
    {
        var centros = await _db.CentrosCostos
            .AsNoTracking()
            .Where(c => c.IdEmpresa == idEmpresa)
            .OrderBy(c => c.Codigo)
            .ToListAsync(cancellationToken);

        return centros.Select(ADto).ToList();
    }

    public async Task<Resultado<CentroCostoDto>> CrearAsync(UsuarioActual usuario, GuardarCentroCostoRequest request, CancellationToken cancellationToken)
    {
        var codigo = Normalizar(request.Codigo);
        if (await CodigoRepetidoAsync(usuario.IdEmpresa, codigo, null, cancellationToken))
        {
            return Repetido(codigo);
        }

        var centro = new CentroCosto { IdEmpresa = usuario.IdEmpresa };
        AplicarCambios(centro, codigo, request);

        _db.CentrosCostos.Add(centro);
        Auditar(usuario, "CREAR", centro);
        await _db.SaveChangesAsync(cancellationToken);

        return Resultado<CentroCostoDto>.Ok(ADto(centro));
    }

    public async Task<Resultado<CentroCostoDto>> EditarAsync(UsuarioActual usuario, int idCentroCosto, GuardarCentroCostoRequest request, CancellationToken cancellationToken)
    {
        var centro = await _db.CentrosCostos
            .FirstOrDefaultAsync(c => c.IdEmpresa == usuario.IdEmpresa && c.IdCentroCosto == idCentroCosto, cancellationToken);

        if (centro is null)
        {
            return Resultado<CentroCostoDto>.Fallo(TipoError.NoEncontrado, "El centro de costo no existe.");
        }

        var codigo = Normalizar(request.Codigo);
        if (await CodigoRepetidoAsync(usuario.IdEmpresa, codigo, idCentroCosto, cancellationToken))
        {
            return Repetido(codigo);
        }

        AplicarCambios(centro, codigo, request);
        Auditar(usuario, "EDITAR", centro);
        await _db.SaveChangesAsync(cancellationToken);

        return Resultado<CentroCostoDto>.Ok(ADto(centro));
    }

    private static string Normalizar(string codigo) => codigo.Trim().ToUpperInvariant();

    private Task<bool> CodigoRepetidoAsync(int idEmpresa, string codigo, int? idCentroCosto, CancellationToken cancellationToken) =>
        _db.CentrosCostos.AnyAsync(
            c => c.IdEmpresa == idEmpresa && c.Codigo == codigo && c.IdCentroCosto != idCentroCosto,
            cancellationToken);

    private static Resultado<CentroCostoDto> Repetido(string codigo) =>
        Resultado<CentroCostoDto>.Fallo(TipoError.Conflicto, $"Ya existe el centro de costo {codigo}.");

    private static void AplicarCambios(CentroCosto centro, string codigo, GuardarCentroCostoRequest request)
    {
        centro.Codigo = codigo;
        centro.Nombre = request.Nombre.Trim();
        centro.Activo = request.Activo;
    }

    private void Auditar(UsuarioActual usuario, string accion, CentroCosto centro)
    {
        _db.AuditoriaLogs.Add(new AuditoriaLog
        {
            IdEmpresa = usuario.IdEmpresa,
            IdUsuario = usuario.IdUsuario,
            Accion = accion,
            TablaAfectada = "centros_costo",
            Detalle = $"{centro.Codigo} {centro.Nombre}",
            IpOrigen = usuario.Ip,
        });
    }

    private static CentroCostoDto ADto(CentroCosto c) => new(c.IdCentroCosto, c.Codigo, c.Nombre, c.Activo ?? true);
}
