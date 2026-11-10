using System.Globalization;
using InfinityLedger.Api.Comun;
using InfinityLedger.Api.Data;
using InfinityLedger.Api.Data.Entities;
using InfinityLedger.Api.Periodos;
using Microsoft.EntityFrameworkCore;

namespace InfinityLedger.Api.Asientos;

public interface IServicioAsientos
{
    Task<IReadOnlyList<AsientoResumenDto>> ListarAsync(int idEmpresa, FiltroAsientos filtro, CancellationToken cancellationToken);

    Task<AsientoDto?> ObtenerAsync(int idEmpresa, int idAsiento, CancellationToken cancellationToken);

    Task<Resultado<AsientoDto>> CrearAsync(UsuarioActual usuario, GuardarAsientoRequest request, CancellationToken cancellationToken);

    Task<Resultado<AsientoDto>> EditarAsync(UsuarioActual usuario, int idAsiento, GuardarAsientoRequest request, CancellationToken cancellationToken);

    Task<Resultado<AsientoDto>> PublicarAsync(UsuarioActual usuario, int idAsiento, CancellationToken cancellationToken);

    Task<Resultado<AsientoDto>> AnularAsync(UsuarioActual usuario, int idAsiento, CancellationToken cancellationToken);
}

/// <summary>
/// Asientos contables. Ciclo de vida: BORRADOR → PUBLICADO → ANULADO (un borrador también se puede anular).
/// Nunca se borran: el número de comprobante se asigna al crear y no debe quedar un hueco.
/// Un borrador puede estar descuadrado; para publicar, débitos y créditos deben ser iguales.
/// </summary>
public class ServicioAsientos : IServicioAsientos
{
    public const string Borrador = "BORRADOR";
    public const string Publicado = "PUBLICADO";
    public const string Anulado = "ANULADO";

    private const int IntentosNumeracion = 3;
    private static readonly CultureInfo Colombia = CultureInfo.GetCultureInfo("es-CO");

    private readonly CodeLedgerContext _db;

    public ServicioAsientos(CodeLedgerContext db)
    {
        _db = db;
    }

    /// <summary>"ING-0004".</summary>
    public static string Comprobante(string codigoTipo, int numero) => $"{codigoTipo}-{numero:D4}";

    public async Task<IReadOnlyList<AsientoResumenDto>> ListarAsync(int idEmpresa, FiltroAsientos filtro, CancellationToken cancellationToken)
    {
        var consulta = _db.AsientosEncabezados.AsNoTracking().Where(a => a.IdEmpresa == idEmpresa);

        if (filtro.IdPeriodo is not null)
        {
            consulta = consulta.Where(a => a.IdPeriodo == filtro.IdPeriodo);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Estado))
        {
            var estado = filtro.Estado.Trim().ToUpperInvariant();
            consulta = consulta.Where(a => a.Estado == estado);
        }

        if (filtro.IdTipoComprobante is not null)
        {
            consulta = consulta.Where(a => a.IdTipoComprobante == filtro.IdTipoComprobante);
        }

        var filas = await consulta
            .OrderByDescending(a => a.FechaAsiento).ThenByDescending(a => a.IdAsiento)
            .Select(a => new
            {
                a.IdAsiento,
                a.IdTipoComprobante,
                CodigoTipo = a.IdTipoComprobanteNavigation.Codigo,
                a.NumeroComprobante,
                a.FechaAsiento,
                a.IdPeriodo,
                a.ConceptoGeneral,
                a.Estado,
                TotalDebe = a.AsientosDetalles.Sum(d => d.Debe ?? 0),
                TotalHaber = a.AsientosDetalles.Sum(d => d.Haber ?? 0),
            })
            .ToListAsync(cancellationToken);

        return filas.Select(f => new AsientoResumenDto(
            f.IdAsiento, f.IdTipoComprobante, Comprobante(f.CodigoTipo, f.NumeroComprobante), f.NumeroComprobante,
            f.FechaAsiento, f.IdPeriodo, f.ConceptoGeneral, f.Estado ?? Publicado, f.TotalDebe, f.TotalHaber)).ToList();
    }

    public async Task<AsientoDto?> ObtenerAsync(int idEmpresa, int idAsiento, CancellationToken cancellationToken)
    {
        var asiento = await _db.AsientosEncabezados
            .AsNoTracking()
            .Include(a => a.IdTipoComprobanteNavigation)
            .Include(a => a.AsientosDetalles).ThenInclude(d => d.IdCuentaNavigation)
            .Include(a => a.AsientosDetalles).ThenInclude(d => d.IdTerceroNavigation)
            .Include(a => a.AsientosDetalles).ThenInclude(d => d.IdCentroCostoNavigation)
            .FirstOrDefaultAsync(a => a.IdEmpresa == idEmpresa && a.IdAsiento == idAsiento, cancellationToken);

        return asiento is null ? null : ADto(asiento);
    }

    public async Task<Resultado<AsientoDto>> CrearAsync(UsuarioActual usuario, GuardarAsientoRequest request, CancellationToken cancellationToken)
    {
        for (var intento = 1; ; intento++)
        {
            var tipo = await _db.ComprobantesTipos
                .FirstOrDefaultAsync(t => t.IdEmpresa == usuario.IdEmpresa && t.IdTipoComprobante == request.IdTipoComprobante, cancellationToken);

            if (tipo is null)
            {
                return Invalido("El tipo de comprobante no existe.");
            }

            var validacion = await ValidarAsync(usuario.IdEmpresa, request.FechaAsiento, request.Lineas, request.Publicar, cancellationToken);
            if (validacion.Error is not null)
            {
                return Invalido(validacion.Error);
            }

            var numero = tipo.ConsecutivoActual ?? 1;
            tipo.ConsecutivoActual = numero + 1;

            var asiento = new AsientoEncabezado
            {
                IdEmpresa = usuario.IdEmpresa,
                IdTipoComprobante = tipo.IdTipoComprobante,
                IdPeriodo = validacion.Periodo!.IdPeriodo,
                IdUsuario = usuario.IdUsuario,
                NumeroComprobante = numero,
                FechaAsiento = request.FechaAsiento,
                ConceptoGeneral = request.ConceptoGeneral.Trim(),
                Estado = request.Publicar ? Publicado : Borrador,
                AsientosDetalles = request.Lineas.Select(ADetalle).ToList(),
            };

            _db.AsientosEncabezados.Add(asiento);
            Auditar(usuario, request.Publicar ? "CREAR_PUBLICADO" : "CREAR_BORRADOR", Comprobante(tipo.Codigo, numero));

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
                return Resultado<AsientoDto>.Ok((await ObtenerAsync(usuario.IdEmpresa, asiento.IdAsiento, cancellationToken))!);
            }
            catch (DbUpdateConcurrencyException) when (intento < IntentosNumeracion)
            {
                // Otro usuario tomó el mismo consecutivo: se descarta lo pendiente y se vuelve a numerar.
                _db.ChangeTracker.Clear();
            }
            catch (DbUpdateConcurrencyException)
            {
                _db.ChangeTracker.Clear();
                return Resultado<AsientoDto>.Fallo(TipoError.Conflicto,
                    "No se pudo asignar el número de comprobante porque otros usuarios están registrando asientos. Intente de nuevo.");
            }
        }
    }

    public async Task<Resultado<AsientoDto>> EditarAsync(UsuarioActual usuario, int idAsiento, GuardarAsientoRequest request, CancellationToken cancellationToken)
    {
        var asiento = await BuscarAsync(usuario.IdEmpresa, idAsiento, cancellationToken);
        if (asiento is null)
        {
            return NoEncontrado();
        }

        if (asiento.Estado != Borrador)
        {
            return Invalido("Solo se pueden modificar asientos en borrador.");
        }

        if (request.IdTipoComprobante != asiento.IdTipoComprobante)
        {
            return Invalido("No se puede cambiar el tipo de comprobante: el asiento ya tiene su número asignado.");
        }

        var validacion = await ValidarAsync(usuario.IdEmpresa, request.FechaAsiento, request.Lineas, request.Publicar, cancellationToken);
        if (validacion.Error is not null)
        {
            return Invalido(validacion.Error);
        }

        _db.AsientosDetalles.RemoveRange(asiento.AsientosDetalles);
        asiento.AsientosDetalles = request.Lineas.Select(ADetalle).ToList();
        asiento.IdPeriodo = validacion.Periodo!.IdPeriodo;
        asiento.FechaAsiento = request.FechaAsiento;
        asiento.ConceptoGeneral = request.ConceptoGeneral.Trim();
        asiento.Estado = request.Publicar ? Publicado : Borrador;

        Auditar(usuario, request.Publicar ? "EDITAR_PUBLICAR" : "EDITAR", NombreComprobante(asiento));
        await _db.SaveChangesAsync(cancellationToken);

        return Resultado<AsientoDto>.Ok((await ObtenerAsync(usuario.IdEmpresa, idAsiento, cancellationToken))!);
    }

    public async Task<Resultado<AsientoDto>> PublicarAsync(UsuarioActual usuario, int idAsiento, CancellationToken cancellationToken)
    {
        var asiento = await BuscarAsync(usuario.IdEmpresa, idAsiento, cancellationToken);
        if (asiento is null)
        {
            return NoEncontrado();
        }

        if (asiento.Estado != Borrador)
        {
            return Invalido("Solo se pueden publicar asientos en borrador.");
        }

        // Se vuelve a validar todo: desde que se guardó el borrador pudo cerrarse el período o desactivarse una cuenta.
        var lineas = asiento.AsientosDetalles.Select(d => new LineaAsientoRequest
        {
            IdCuenta = d.IdCuenta,
            IdTercero = d.IdTercero,
            IdCentroCosto = d.IdCentroCosto,
            Concepto = d.Concepto,
            Debe = d.Debe ?? 0,
            Haber = d.Haber ?? 0,
        }).ToList();

        var validacion = await ValidarAsync(usuario.IdEmpresa, asiento.FechaAsiento, lineas, exigirCuadre: true, cancellationToken);
        if (validacion.Error is not null)
        {
            return Invalido(validacion.Error);
        }

        asiento.Estado = Publicado;
        Auditar(usuario, "PUBLICAR", NombreComprobante(asiento));
        await _db.SaveChangesAsync(cancellationToken);

        return Resultado<AsientoDto>.Ok((await ObtenerAsync(usuario.IdEmpresa, idAsiento, cancellationToken))!);
    }

    public async Task<Resultado<AsientoDto>> AnularAsync(UsuarioActual usuario, int idAsiento, CancellationToken cancellationToken)
    {
        var asiento = await BuscarAsync(usuario.IdEmpresa, idAsiento, cancellationToken);
        if (asiento is null)
        {
            return NoEncontrado();
        }

        if (asiento.Estado == Anulado)
        {
            return Invalido("El asiento ya está anulado.");
        }

        var periodo = asiento.IdPeriodoNavigation;
        if (periodo.Estado == ServicioPeriodos.Cerrado)
        {
            return Invalido($"El período {ServicioPeriodos.Nombre(periodo.Anio, periodo.Mes)} está cerrado; un administrador debe reabrirlo para anular.");
        }

        asiento.Estado = Anulado;
        Auditar(usuario, "ANULAR", NombreComprobante(asiento));
        await _db.SaveChangesAsync(cancellationToken);

        return Resultado<AsientoDto>.Ok((await ObtenerAsync(usuario.IdEmpresa, idAsiento, cancellationToken))!);
    }

    private sealed record Validacion(PeriodoFiscal? Periodo, string? Error);

    private async Task<Validacion> ValidarAsync(
        int idEmpresa, DateOnly fecha, IReadOnlyList<LineaAsientoRequest> lineas, bool exigirCuadre, CancellationToken cancellationToken)
    {
        var periodo = await _db.PeriodosFiscales
            .FirstOrDefaultAsync(p => p.IdEmpresa == idEmpresa && p.FechaInicio <= fecha && p.FechaFin >= fecha, cancellationToken);

        if (periodo is null)
        {
            return new(null, $"No existe un período fiscal para el {fecha.ToString("dd/MM/yyyy", Colombia)}.");
        }

        if (periodo.Estado == ServicioPeriodos.Cerrado)
        {
            return new(null, $"El período {ServicioPeriodos.Nombre(periodo.Anio, periodo.Mes)} está cerrado.");
        }

        var error = await ValidarLineasAsync(idEmpresa, lineas, exigirCuadre, cancellationToken);
        return new(error is null ? periodo : null, error);
    }

    private async Task<string?> ValidarLineasAsync(
        int idEmpresa, IReadOnlyList<LineaAsientoRequest> lineas, bool exigirCuadre, CancellationToken cancellationToken)
    {
        if (lineas.Count == 0)
        {
            return "El asiento debe tener al menos una línea.";
        }

        var idsCuentas = lineas.Select(l => l.IdCuenta).Distinct().ToList();
        var idsTerceros = lineas.Where(l => l.IdTercero is not null).Select(l => l.IdTercero!.Value).Distinct().ToList();
        var idsCentros = lineas.Where(l => l.IdCentroCosto is not null).Select(l => l.IdCentroCosto!.Value).Distinct().ToList();

        var cuentas = await _db.PlanCuentas
            .Where(c => c.IdEmpresa == idEmpresa && idsCuentas.Contains(c.IdCuenta))
            .ToDictionaryAsync(c => c.IdCuenta, cancellationToken);
        var terceros = await _db.Terceros
            .Where(t => t.IdEmpresa == idEmpresa && idsTerceros.Contains(t.IdTercero))
            .ToDictionaryAsync(t => t.IdTercero, cancellationToken);
        var centros = await _db.CentrosCostos
            .Where(c => c.IdEmpresa == idEmpresa && idsCentros.Contains(c.IdCentroCosto))
            .ToDictionaryAsync(c => c.IdCentroCosto, cancellationToken);

        for (var i = 0; i < lineas.Count; i++)
        {
            var linea = lineas[i];
            var n = i + 1;

            if (!cuentas.TryGetValue(linea.IdCuenta, out var cuenta))
            {
                return $"Línea {n}: la cuenta no existe.";
            }

            if (cuenta.Activo == false)
            {
                return $"Línea {n}: la cuenta {cuenta.Codigo} está inactiva.";
            }

            if (cuenta.EsMovimiento != true)
            {
                return $"Línea {n}: la cuenta {cuenta.Codigo} no es auxiliar; use una subcuenta que reciba movimientos.";
            }

            if ((linea.Debe > 0) == (linea.Haber > 0))
            {
                return $"Línea {n}: registre el valor en Débito o en Crédito, no en ambos ni en ninguno.";
            }

            if (decimal.Round(linea.Debe, 2) != linea.Debe || decimal.Round(linea.Haber, 2) != linea.Haber)
            {
                return $"Línea {n}: los valores admiten máximo 2 decimales.";
            }

            if (linea.IdTercero is not null)
            {
                if (!terceros.TryGetValue(linea.IdTercero.Value, out var tercero))
                {
                    return $"Línea {n}: el tercero no existe.";
                }

                if (tercero.Estado == false)
                {
                    return $"Línea {n}: el tercero {tercero.NombreRazonSocial} está inactivo.";
                }
            }
            else if (cuenta.PideTercero == true)
            {
                return $"Línea {n}: la cuenta {cuenta.Codigo} exige un tercero.";
            }

            if (linea.IdCentroCosto is not null)
            {
                if (!centros.TryGetValue(linea.IdCentroCosto.Value, out var centro))
                {
                    return $"Línea {n}: el centro de costo no existe.";
                }

                if (centro.Activo == false)
                {
                    return $"Línea {n}: el centro de costo {centro.Codigo} está inactivo.";
                }
            }
            else if (cuenta.PideCentroCosto == true)
            {
                return $"Línea {n}: la cuenta {cuenta.Codigo} exige un centro de costo.";
            }
        }

        if (!exigirCuadre)
        {
            return null;
        }

        if (lineas.Count < 2)
        {
            return "Para publicar, el asiento necesita al menos dos líneas.";
        }

        var totalDebe = lineas.Sum(l => l.Debe);
        var totalHaber = lineas.Sum(l => l.Haber);
        if (totalDebe != totalHaber)
        {
            return string.Format(Colombia,
                "El asiento no cuadra: débitos {0:N2} y créditos {1:N2} (diferencia {2:N2}).",
                totalDebe, totalHaber, Math.Abs(totalDebe - totalHaber));
        }

        return null;
    }

    private Task<AsientoEncabezado?> BuscarAsync(int idEmpresa, int idAsiento, CancellationToken cancellationToken) =>
        _db.AsientosEncabezados
            .Include(a => a.AsientosDetalles)
            .Include(a => a.IdPeriodoNavigation)
            .Include(a => a.IdTipoComprobanteNavigation)
            .FirstOrDefaultAsync(a => a.IdEmpresa == idEmpresa && a.IdAsiento == idAsiento, cancellationToken);

    private static string NombreComprobante(AsientoEncabezado a) => Comprobante(a.IdTipoComprobanteNavigation.Codigo, a.NumeroComprobante);

    private static AsientoDetalle ADetalle(LineaAsientoRequest l) => new()
    {
        IdCuenta = l.IdCuenta,
        IdTercero = l.IdTercero,
        IdCentroCosto = l.IdCentroCosto,
        Concepto = string.IsNullOrWhiteSpace(l.Concepto) ? null : l.Concepto.Trim(),
        Debe = l.Debe,
        Haber = l.Haber,
    };

    private void Auditar(UsuarioActual usuario, string accion, string comprobante)
    {
        _db.AuditoriaLogs.Add(new AuditoriaLog
        {
            IdEmpresa = usuario.IdEmpresa,
            IdUsuario = usuario.IdUsuario,
            Accion = accion,
            TablaAfectada = "asientos_encabezado",
            Detalle = comprobante,
            IpOrigen = usuario.Ip,
        });
    }

    private static Resultado<AsientoDto> Invalido(string mensaje) => Resultado<AsientoDto>.Fallo(TipoError.Validacion, mensaje);

    private static Resultado<AsientoDto> NoEncontrado() => Resultado<AsientoDto>.Fallo(TipoError.NoEncontrado, "El asiento no existe.");

    private static AsientoDto ADto(AsientoEncabezado a)
    {
        var lineas = a.AsientosDetalles
            .OrderBy(d => d.IdAsientoDetalle)
            .Select(d => new LineaAsientoDto(
                d.IdAsientoDetalle,
                d.IdCuenta,
                d.IdCuentaNavigation.Codigo,
                d.IdCuentaNavigation.NombreCuenta,
                d.IdTercero,
                d.IdTerceroNavigation?.NombreRazonSocial,
                d.IdCentroCosto,
                d.IdCentroCostoNavigation?.Codigo,
                d.Concepto,
                d.Debe ?? 0,
                d.Haber ?? 0))
            .ToList();

        return new AsientoDto(
            a.IdAsiento,
            a.IdTipoComprobante,
            Comprobante(a.IdTipoComprobanteNavigation.Codigo, a.NumeroComprobante),
            a.NumeroComprobante,
            a.FechaAsiento,
            a.IdPeriodo,
            a.ConceptoGeneral,
            a.Estado ?? Publicado,
            lineas.Sum(l => l.Debe),
            lineas.Sum(l => l.Haber),
            lineas);
    }
}
