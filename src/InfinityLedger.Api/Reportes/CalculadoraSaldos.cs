using InfinityLedger.Api.Asientos;
using InfinityLedger.Api.Data;
using InfinityLedger.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace InfinityLedger.Api.Reportes;

/// <summary>Saldo y movimientos de una cuenta en un rango; en las cuentas mayores incluye a todas sus subcuentas.</summary>
public sealed class SaldoCuenta
{
    public required PlanCuenta Cuenta { get; init; }

    /// <summary>Débitos y créditos anteriores al inicio del rango.</summary>
    public decimal DebeAnterior { get; set; }

    public decimal HaberAnterior { get; set; }

    /// <summary>Débitos y créditos dentro del rango.</summary>
    public decimal Debe { get; set; }

    public decimal Haber { get; set; }

    public decimal SaldoInicial => CalculadoraSaldos.Saldo(Cuenta.Naturaleza, DebeAnterior, HaberAnterior);

    public decimal SaldoFinal => CalculadoraSaldos.Saldo(Cuenta.Naturaleza, DebeAnterior + Debe, HaberAnterior + Haber);

    public bool TieneValores => DebeAnterior != 0 || HaberAnterior != 0 || Debe != 0 || Haber != 0;
}

/// <summary>
/// Base de todos los reportes. Solo cuenta asientos PUBLICADOS (ni borradores ni anulados) y
/// acumula lo de cada auxiliar en todas sus cuentas padre.
/// </summary>
public class CalculadoraSaldos
{
    private readonly CodeLedgerContext _db;

    public CalculadoraSaldos(CodeLedgerContext db)
    {
        _db = db;
    }

    /// <summary>Saldo con el signo de la naturaleza: positivo es saldo normal; negativo, saldo contrario.</summary>
    public static decimal Saldo(string naturaleza, decimal debe, decimal haber) =>
        naturaleza == "CREDITO" ? haber - debe : debe - haber;

    /// <summary>
    /// Saldos de todas las cuentas de la empresa, ordenadas por código.
    /// <paramref name="desde"/> null: sin saldo anterior, todo lo publicado hasta <paramref name="hasta"/> cuenta como movimiento.
    /// </summary>
    public async Task<IReadOnlyList<SaldoCuenta>> SaldosAsync(int idEmpresa, DateOnly? desde, DateOnly hasta, CancellationToken cancellationToken)
    {
        var cuentas = await _db.PlanCuentas
            .AsNoTracking()
            .Where(c => c.IdEmpresa == idEmpresa)
            .OrderBy(c => c.Codigo)
            .ToListAsync(cancellationToken);

        var saldos = cuentas.ToDictionary(c => c.IdCuenta, c => new SaldoCuenta { Cuenta = c });

        if (desde is not null)
        {
            var anteriores = await MovimientosAsync(idEmpresa, null, desde.Value.AddDays(-1), cancellationToken);
            Acumular(saldos, anteriores, (s, debe, haber) => { s.DebeAnterior += debe; s.HaberAnterior += haber; });
        }

        var delRango = await MovimientosAsync(idEmpresa, desde, hasta, cancellationToken);
        Acumular(saldos, delRango, (s, debe, haber) => { s.Debe += debe; s.Haber += haber; });

        return saldos.Values.OrderBy(s => s.Cuenta.Codigo, StringComparer.Ordinal).ToList();
    }

    /// <summary>Débitos y créditos publicados por cuenta auxiliar en el rango (límites incluidos; null = sin límite).</summary>
    public async Task<Dictionary<int, (decimal Debe, decimal Haber)>> MovimientosAsync(
        int idEmpresa, DateOnly? desde, DateOnly? hasta, CancellationToken cancellationToken)
    {
        var lineas = LineasPublicadas(idEmpresa, desde, hasta);

        var totales = await lineas
            .GroupBy(d => d.IdCuenta)
            .Select(g => new { IdCuenta = g.Key, Debe = g.Sum(d => d.Debe ?? 0), Haber = g.Sum(d => d.Haber ?? 0) })
            .ToListAsync(cancellationToken);

        return totales.ToDictionary(t => t.IdCuenta, t => (t.Debe, t.Haber));
    }

    /// <summary>Líneas de asientos publicados de la empresa en el rango, listas para filtrar o agrupar.</summary>
    public IQueryable<AsientoDetalle> LineasPublicadas(int idEmpresa, DateOnly? desde, DateOnly? hasta)
    {
        var lineas = _db.AsientosDetalles
            .AsNoTracking()
            .Where(d => d.IdAsientoNavigation.IdEmpresa == idEmpresa && d.IdAsientoNavigation.Estado == ServicioAsientos.Publicado);

        if (desde is not null)
        {
            var d = desde.Value;
            lineas = lineas.Where(l => l.IdAsientoNavigation.FechaAsiento >= d);
        }

        if (hasta is not null)
        {
            var h = hasta.Value;
            lineas = lineas.Where(l => l.IdAsientoNavigation.FechaAsiento <= h);
        }

        return lineas;
    }

    /// <summary>Suma cada movimiento en su cuenta y en todas sus cuentas padre.</summary>
    private static void Acumular(
        Dictionary<int, SaldoCuenta> saldos,
        Dictionary<int, (decimal Debe, decimal Haber)> movimientos,
        Action<SaldoCuenta, decimal, decimal> sumar)
    {
        foreach (var (idCuenta, (debe, haber)) in movimientos)
        {
            int? actual = idCuenta;
            var visitadas = new HashSet<int>();
            while (actual is not null && saldos.TryGetValue(actual.Value, out var saldo) && visitadas.Add(actual.Value))
            {
                sumar(saldo, debe, haber);
                actual = saldo.Cuenta.IdCuentaPadre;
            }
        }
    }
}
