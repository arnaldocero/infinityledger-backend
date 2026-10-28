using InfinityLedger.Api.Comun;
using InfinityLedger.Api.Data;
using InfinityLedger.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace InfinityLedger.Api.PlanCuentas;

public interface IServicioPlanCuentas
{
    Task<IReadOnlyList<CuentaDto>> ListarAsync(int idEmpresa, CancellationToken cancellationToken);

    Task<CuentaDto?> ObtenerAsync(int idEmpresa, int idCuenta, CancellationToken cancellationToken);

    Task<Resultado<CuentaDto>> CrearAsync(UsuarioActual usuario, CrearCuentaRequest request, CancellationToken cancellationToken);

    Task<Resultado<CuentaDto>> EditarAsync(UsuarioActual usuario, int idCuenta, EditarCuentaRequest request, CancellationToken cancellationToken);
}

/// <summary>Plan de cuentas de la empresa del usuario. No hay borrado: una cuenta se desactiva.</summary>
public class ServicioPlanCuentas : IServicioPlanCuentas
{
    private readonly CodeLedgerContext _db;

    public ServicioPlanCuentas(CodeLedgerContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CuentaDto>> ListarAsync(int idEmpresa, CancellationToken cancellationToken)
    {
        var cuentas = await _db.PlanCuentas
            .AsNoTracking()
            .Where(c => c.IdEmpresa == idEmpresa)
            .OrderBy(c => c.Codigo)
            .ToListAsync(cancellationToken);

        return cuentas.Select(ADto).ToList();
    }

    public async Task<CuentaDto?> ObtenerAsync(int idEmpresa, int idCuenta, CancellationToken cancellationToken)
    {
        var cuenta = await _db.PlanCuentas
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.IdEmpresa == idEmpresa && c.IdCuenta == idCuenta, cancellationToken);

        return cuenta is null ? null : ADto(cuenta);
    }

    public async Task<Resultado<CuentaDto>> CrearAsync(UsuarioActual usuario, CrearCuentaRequest request, CancellationToken cancellationToken)
    {
        var codigo = request.Codigo.Trim();

        var nivel = ReglasPuc.NivelPorCodigo(codigo);
        if (nivel is null)
        {
            return Invalido("El código debe tener 1, 2, 4 o 6 dígitos.");
        }

        var errorValores = ValidarValores(request);
        if (errorValores is not null)
        {
            return Invalido(errorValores);
        }

        if (await _db.PlanCuentas.AnyAsync(c => c.IdEmpresa == usuario.IdEmpresa && c.Codigo == codigo, cancellationToken))
        {
            return Resultado<CuentaDto>.Fallo(TipoError.Conflicto, $"Ya existe la cuenta {codigo}.");
        }

        PlanCuenta? padre = null;
        var codigoPadre = ReglasPuc.CodigoPadre(codigo);
        if (codigoPadre is not null)
        {
            padre = await _db.PlanCuentas
                .FirstOrDefaultAsync(c => c.IdEmpresa == usuario.IdEmpresa && c.Codigo == codigoPadre, cancellationToken);

            var errorPadre = ValidarPadre(padre, codigoPadre, request.TipoCuenta);
            if (errorPadre is not null)
            {
                return Invalido(errorPadre);
            }
        }

        var cuenta = new PlanCuenta
        {
            IdEmpresa = usuario.IdEmpresa,
            Codigo = codigo,
            IdCuentaPadre = padre?.IdCuenta,
            Nivel = nivel.Value,
        };
        AplicarCambios(cuenta, request);

        _db.PlanCuentas.Add(cuenta);
        Auditar(usuario, "CREAR", cuenta);
        await _db.SaveChangesAsync(cancellationToken);

        return Resultado<CuentaDto>.Ok(ADto(cuenta));
    }

    public async Task<Resultado<CuentaDto>> EditarAsync(UsuarioActual usuario, int idCuenta, EditarCuentaRequest request, CancellationToken cancellationToken)
    {
        var cuenta = await _db.PlanCuentas
            .Include(c => c.IdCuentaPadreNavigation)
            .FirstOrDefaultAsync(c => c.IdEmpresa == usuario.IdEmpresa && c.IdCuenta == idCuenta, cancellationToken);

        if (cuenta is null)
        {
            return Resultado<CuentaDto>.Fallo(TipoError.NoEncontrado, "La cuenta no existe.");
        }

        var errorValores = ValidarValores(request);
        if (errorValores is not null)
        {
            return Invalido(errorValores);
        }

        var padre = cuenta.IdCuentaPadreNavigation;
        if (padre is not null && request.TipoCuenta != padre.TipoCuenta)
        {
            return Invalido($"El tipo debe ser el mismo de la cuenta {padre.Codigo} ({padre.TipoCuenta}).");
        }

        var hijas = await _db.PlanCuentas
            .Where(c => c.IdCuentaPadre == cuenta.IdCuenta)
            .Select(c => new { c.TipoCuenta, c.Activo })
            .ToListAsync(cancellationToken);

        if (hijas.Count > 0)
        {
            if (request.EsMovimiento)
            {
                return Invalido("Una cuenta con subcuentas no puede ser auxiliar (recibir movimientos).");
            }

            if (request.TipoCuenta != cuenta.TipoCuenta)
            {
                return Invalido("No se puede cambiar el tipo de una cuenta que tiene subcuentas.");
            }

            if (!request.Activo && hijas.Any(h => h.Activo != false))
            {
                return Invalido("Primero desactive sus subcuentas.");
            }
        }

        if (cuenta.EsMovimiento == true && !request.EsMovimiento
            && await _db.AsientosDetalles.AnyAsync(d => d.IdCuenta == cuenta.IdCuenta, cancellationToken))
        {
            return Invalido("La cuenta ya tiene movimientos registrados; debe seguir siendo auxiliar.");
        }

        AplicarCambios(cuenta, request);
        Auditar(usuario, "EDITAR", cuenta);
        await _db.SaveChangesAsync(cancellationToken);

        return Resultado<CuentaDto>.Ok(ADto(cuenta));
    }

    private static string? ValidarValores(EditarCuentaRequest request)
    {
        if (!ReglasPuc.TiposCuenta.Contains(request.TipoCuenta))
        {
            return $"Tipo de cuenta no válido: {request.TipoCuenta}.";
        }

        if (!ReglasPuc.Naturalezas.Contains(request.Naturaleza))
        {
            return $"Naturaleza no válida: {request.Naturaleza}.";
        }

        return null;
    }

    private static string? ValidarPadre(PlanCuenta? padre, string codigoPadre, string tipoCuenta)
    {
        if (padre is null)
        {
            return $"Primero cree la cuenta {codigoPadre}.";
        }

        if (padre.Activo == false)
        {
            return $"La cuenta {codigoPadre} está inactiva.";
        }

        if (padre.EsMovimiento == true)
        {
            return $"La cuenta {codigoPadre} es auxiliar (recibe movimientos) y no puede tener subcuentas.";
        }

        if (padre.TipoCuenta != tipoCuenta)
        {
            return $"El tipo debe ser el mismo de la cuenta {codigoPadre} ({padre.TipoCuenta}).";
        }

        return null;
    }

    private static void AplicarCambios(PlanCuenta cuenta, EditarCuentaRequest request)
    {
        cuenta.NombreCuenta = request.NombreCuenta.Trim();
        cuenta.TipoCuenta = request.TipoCuenta;
        cuenta.Naturaleza = request.Naturaleza;
        cuenta.EsMovimiento = request.EsMovimiento;
        cuenta.PideTercero = request.PideTercero;
        cuenta.PideCentroCosto = request.PideCentroCosto;
        cuenta.Activo = request.Activo;
    }

    private void Auditar(UsuarioActual usuario, string accion, PlanCuenta cuenta)
    {
        _db.AuditoriaLogs.Add(new AuditoriaLog
        {
            IdEmpresa = usuario.IdEmpresa,
            IdUsuario = usuario.IdUsuario,
            Accion = accion,
            TablaAfectada = "plan_cuentas",
            Detalle = $"{cuenta.Codigo} {cuenta.NombreCuenta}",
            IpOrigen = usuario.Ip,
        });
    }

    private static Resultado<CuentaDto> Invalido(string mensaje) => Resultado<CuentaDto>.Fallo(TipoError.Validacion, mensaje);

    private static CuentaDto ADto(PlanCuenta c) => new(
        c.IdCuenta,
        c.Codigo,
        c.NombreCuenta,
        c.IdCuentaPadre,
        c.Nivel,
        c.TipoCuenta,
        c.Naturaleza,
        c.EsMovimiento ?? false,
        c.PideTercero ?? false,
        c.PideCentroCosto ?? false,
        c.Activo ?? true);
}
