using InfinityLedger.Api.Asientos;
using InfinityLedger.Api.Comun;
using InfinityLedger.Api.Data;
using InfinityLedger.Api.Periodos;
using Microsoft.EntityFrameworkCore;

namespace InfinityLedger.Api.Reportes;

public interface IServicioReportes
{
    Task<Resultado<SaldosMensualesDto>> SaldosMensualesAsync(int idEmpresa, int idPeriodo, CancellationToken cancellationToken);

    Task<Resultado<LibroDiarioDto>> LibroDiarioAsync(int idEmpresa, DateOnly desde, DateOnly hasta, CancellationToken cancellationToken);

    Task<Resultado<LibroMayorDto>> LibroMayorAsync(int idEmpresa, int idCuenta, DateOnly desde, DateOnly hasta, CancellationToken cancellationToken);

    Task<Resultado<BalanceComprobacionDto>> BalanceComprobacionAsync(int idEmpresa, DateOnly desde, DateOnly hasta, int nivelMaximo, CancellationToken cancellationToken);

    Task<Resultado<EstadoResultadosDto>> EstadoResultadosAsync(int idEmpresa, DateOnly desde, DateOnly hasta, int nivelMaximo, CancellationToken cancellationToken);

    Task<Resultado<BalanceGeneralDto>> BalanceGeneralAsync(int idEmpresa, DateOnly fechaCorte, int nivelMaximo, CancellationToken cancellationToken);

    Task<DashboardDto> DashboardAsync(int idEmpresa, CancellationToken cancellationToken);
}

/// <summary>Reportes contables. Todos usan solo asientos publicados (ver <see cref="CalculadoraSaldos"/>).</summary>
public class ServicioReportes : IServicioReportes
{
    private static readonly HashSet<string> TiposCosto = ["COSTO_VENTAS", "COSTO_PRODUCCION"];
    private static readonly HashSet<string> TiposResultado = ["INGRESO", "GASTO", "COSTO_VENTAS", "COSTO_PRODUCCION"];

    private readonly CodeLedgerContext _db;
    private readonly CalculadoraSaldos _calculadora;
    private readonly IServicioAsientos _asientos;
    private readonly TimeProvider _reloj;

    public ServicioReportes(CodeLedgerContext db, IServicioAsientos asientos, TimeProvider reloj)
    {
        _db = db;
        _calculadora = new CalculadoraSaldos(db);
        _asientos = asientos;
        _reloj = reloj;
    }

    public async Task<Resultado<SaldosMensualesDto>> SaldosMensualesAsync(int idEmpresa, int idPeriodo, CancellationToken cancellationToken)
    {
        var periodo = await _db.PeriodosFiscales.AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdEmpresa == idEmpresa && p.IdPeriodo == idPeriodo, cancellationToken);

        if (periodo is null)
        {
            return Resultado<SaldosMensualesDto>.Fallo(TipoError.NoEncontrado, "El período no existe.");
        }

        var cerrado = periodo.Estado == ServicioPeriodos.Cerrado;
        List<FilaCuentaDto> filas;

        if (cerrado)
        {
            // Lo que se guardó al cerrar el período.
            filas = await _db.SaldosMensuales.AsNoTracking()
                .Where(s => s.IdEmpresa == idEmpresa && s.IdPeriodo == idPeriodo)
                .Select(s => new FilaCuentaDto(
                    s.IdCuenta, s.IdCuentaNavigation.Codigo, s.IdCuentaNavigation.NombreCuenta, s.IdCuentaNavigation.Nivel,
                    s.IdCuentaNavigation.TipoCuenta, s.IdCuentaNavigation.Naturaleza, true,
                    s.SaldoInicial ?? 0, s.TotalDebe ?? 0, s.TotalHaber ?? 0, s.SaldoFinal ?? 0))
                .ToListAsync(cancellationToken);
            filas = filas.OrderBy(f => f.Codigo, StringComparer.Ordinal).ToList();
        }
        else
        {
            var saldos = await _calculadora.SaldosAsync(idEmpresa, periodo.FechaInicio, periodo.FechaFin, cancellationToken);
            filas = saldos.Where(s => s.Cuenta.EsMovimiento == true && s.TieneValores).Select(AFila).ToList();
        }

        return Resultado<SaldosMensualesDto>.Ok(new SaldosMensualesDto(
            periodo.IdPeriodo,
            ServicioPeriodos.Nombre(periodo.Anio, periodo.Mes),
            cerrado,
            filas,
            filas.Sum(f => f.Debitos),
            filas.Sum(f => f.Creditos)));
    }

    public async Task<Resultado<LibroDiarioDto>> LibroDiarioAsync(int idEmpresa, DateOnly desde, DateOnly hasta, CancellationToken cancellationToken)
    {
        if (desde > hasta)
        {
            return RangoInvalido<LibroDiarioDto>();
        }

        var asientos = await _db.AsientosEncabezados
            .AsNoTracking()
            .Where(a => a.IdEmpresa == idEmpresa && a.Estado == ServicioAsientos.Publicado
                && a.FechaAsiento >= desde && a.FechaAsiento <= hasta)
            .Include(a => a.IdTipoComprobanteNavigation)
            .Include(a => a.AsientosDetalles).ThenInclude(d => d.IdCuentaNavigation)
            .Include(a => a.AsientosDetalles).ThenInclude(d => d.IdTerceroNavigation)
            .Include(a => a.AsientosDetalles).ThenInclude(d => d.IdCentroCostoNavigation)
            .OrderBy(a => a.FechaAsiento).ThenBy(a => a.IdTipoComprobanteNavigation.Codigo).ThenBy(a => a.NumeroComprobante)
            .ToListAsync(cancellationToken);

        var dtos = asientos.Select(ServicioAsientos.ADto).ToList();
        return Resultado<LibroDiarioDto>.Ok(new LibroDiarioDto(desde, hasta, dtos, dtos.Sum(a => a.TotalDebe), dtos.Sum(a => a.TotalHaber)));
    }

    public async Task<Resultado<LibroMayorDto>> LibroMayorAsync(int idEmpresa, int idCuenta, DateOnly desde, DateOnly hasta, CancellationToken cancellationToken)
    {
        if (desde > hasta)
        {
            return RangoInvalido<LibroMayorDto>();
        }

        var cuentas = await _db.PlanCuentas.AsNoTracking().Where(c => c.IdEmpresa == idEmpresa).ToListAsync(cancellationToken);
        var cuenta = cuentas.FirstOrDefault(c => c.IdCuenta == idCuenta);
        if (cuenta is null)
        {
            return Resultado<LibroMayorDto>.Fallo(TipoError.NoEncontrado, "La cuenta no existe.");
        }

        // Una cuenta mayor incluye los movimientos de todas sus subcuentas.
        var ids = new HashSet<int> { cuenta.IdCuenta };
        for (var agregadas = true; agregadas;)
        {
            var hijas = cuentas.Where(c => c.IdCuentaPadre is not null && ids.Contains(c.IdCuentaPadre.Value) && !ids.Contains(c.IdCuenta)).ToList();
            hijas.ForEach(c => ids.Add(c.IdCuenta));
            agregadas = hijas.Count > 0;
        }

        var anteriores = await _calculadora.MovimientosAsync(idEmpresa, null, desde.AddDays(-1), cancellationToken);
        var debeAnterior = anteriores.Where(m => ids.Contains(m.Key)).Sum(m => m.Value.Debe);
        var haberAnterior = anteriores.Where(m => ids.Contains(m.Key)).Sum(m => m.Value.Haber);
        var saldoInicial = CalculadoraSaldos.Saldo(cuenta.Naturaleza, debeAnterior, haberAnterior);

        var lineas = await _calculadora.LineasPublicadas(idEmpresa, desde, hasta)
            .Where(d => ids.Contains(d.IdCuenta))
            .OrderBy(d => d.IdAsientoNavigation.FechaAsiento)
            .ThenBy(d => d.IdAsientoNavigation.IdTipoComprobanteNavigation.Codigo)
            .ThenBy(d => d.IdAsientoNavigation.NumeroComprobante)
            .ThenBy(d => d.IdAsientoDetalle)
            .Select(d => new
            {
                d.IdAsientoNavigation.FechaAsiento,
                d.IdAsiento,
                CodigoTipo = d.IdAsientoNavigation.IdTipoComprobanteNavigation.Codigo,
                d.IdAsientoNavigation.NumeroComprobante,
                Concepto = d.Concepto ?? d.IdAsientoNavigation.ConceptoGeneral,
                CodigoCuenta = d.IdCuentaNavigation.Codigo,
                NombreTercero = d.IdTerceroNavigation != null ? d.IdTerceroNavigation.NombreRazonSocial : null,
                Debe = d.Debe ?? 0,
                Haber = d.Haber ?? 0,
            })
            .ToListAsync(cancellationToken);

        var saldo = saldoInicial;
        var movimientos = lineas.Select(l =>
        {
            saldo += CalculadoraSaldos.Saldo(cuenta.Naturaleza, l.Debe, l.Haber);
            return new MovimientoMayorDto(l.FechaAsiento, l.IdAsiento, ServicioAsientos.Comprobante(l.CodigoTipo, l.NumeroComprobante),
                l.Concepto, l.CodigoCuenta, l.NombreTercero, l.Debe, l.Haber, saldo);
        }).ToList();

        return Resultado<LibroMayorDto>.Ok(new LibroMayorDto(
            cuenta.IdCuenta, cuenta.Codigo, cuenta.NombreCuenta, cuenta.Naturaleza, desde, hasta,
            saldoInicial, movimientos, movimientos.Sum(m => m.Debe), movimientos.Sum(m => m.Haber), saldo));
    }

    public async Task<Resultado<BalanceComprobacionDto>> BalanceComprobacionAsync(
        int idEmpresa, DateOnly desde, DateOnly hasta, int nivelMaximo, CancellationToken cancellationToken)
    {
        if (desde > hasta)
        {
            return RangoInvalido<BalanceComprobacionDto>();
        }

        var saldos = await _calculadora.SaldosAsync(idEmpresa, desde, hasta, cancellationToken);
        var filas = saldos.Where(s => s.TieneValores && s.Cuenta.Nivel <= nivelMaximo).Select(AFila).ToList();

        // Los totales salen de las auxiliares: sumar también las mayores contaría dos veces.
        var auxiliares = saldos.Where(s => s.Cuenta.EsMovimiento == true).ToList();
        var totalDebitos = auxiliares.Sum(s => s.Debe);
        var totalCreditos = auxiliares.Sum(s => s.Haber);

        return Resultado<BalanceComprobacionDto>.Ok(new BalanceComprobacionDto(
            desde, hasta, nivelMaximo, filas, totalDebitos, totalCreditos, totalDebitos == totalCreditos));
    }

    public async Task<Resultado<EstadoResultadosDto>> EstadoResultadosAsync(
        int idEmpresa, DateOnly desde, DateOnly hasta, int nivelMaximo, CancellationToken cancellationToken)
    {
        if (desde > hasta)
        {
            return RangoInvalido<EstadoResultadosDto>();
        }

        var saldos = await _calculadora.SaldosAsync(idEmpresa, desde, hasta, cancellationToken);
        var conMovimiento = saldos.Where(s => s.Debe != 0 || s.Haber != 0).ToList();

        var ingresos = Seccion("Ingresos", conMovimiento, nivelMaximo, s => s.Cuenta.TipoCuenta == "INGRESO", s => s.Haber - s.Debe);
        var costos = Seccion("Costos", conMovimiento, nivelMaximo, s => TiposCosto.Contains(s.Cuenta.TipoCuenta), s => s.Debe - s.Haber);
        var gastos = Seccion("Gastos", conMovimiento, nivelMaximo, s => s.Cuenta.TipoCuenta == "GASTO", s => s.Debe - s.Haber);

        var utilidadBruta = ingresos.Total - costos.Total;
        return Resultado<EstadoResultadosDto>.Ok(new EstadoResultadosDto(
            desde, hasta, ingresos, costos, gastos, utilidadBruta, utilidadBruta - gastos.Total));
    }

    public async Task<Resultado<BalanceGeneralDto>> BalanceGeneralAsync(int idEmpresa, DateOnly fechaCorte, int nivelMaximo, CancellationToken cancellationToken)
    {
        // Saldos acumulados desde el primer asiento hasta el corte.
        var saldos = await _calculadora.SaldosAsync(idEmpresa, null, fechaCorte, cancellationToken);
        var conSaldo = saldos.Where(s => s.Debe != 0 || s.Haber != 0).ToList();

        var activo = Seccion("Activo", conSaldo, nivelMaximo, s => s.Cuenta.TipoCuenta == "ACTIVO", s => s.Debe - s.Haber);
        var pasivo = Seccion("Pasivo", conSaldo, nivelMaximo, s => s.Cuenta.TipoCuenta == "PASIVO", s => s.Haber - s.Debe);
        var patrimonio = Seccion("Patrimonio", conSaldo, nivelMaximo, s => s.Cuenta.TipoCuenta == "PATRIMONIO", s => s.Haber - s.Debe);

        // Ingresos menos costos y gastos que aún no se han trasladado al patrimonio con un asiento de cierre.
        var tipoPorCuenta = saldos.ToDictionary(s => s.Cuenta.IdCuenta, s => s.Cuenta.TipoCuenta);
        var inicioAnio = new DateOnly(fechaCorte.Year, 1, 1);
        var resultadoEjercicio = Resultado(await _calculadora.MovimientosAsync(idEmpresa, inicioAnio, fechaCorte, cancellationToken), tipoPorCuenta);
        var resultadosAnteriores = Resultado(await _calculadora.MovimientosAsync(idEmpresa, null, inicioAnio.AddDays(-1), cancellationToken), tipoPorCuenta);

        var totalPatrimonio = patrimonio.Total + resultadoEjercicio + resultadosAnteriores;
        var totalPasivoPatrimonio = pasivo.Total + totalPatrimonio;
        var diferencia = activo.Total - totalPasivoPatrimonio;

        return Resultado<BalanceGeneralDto>.Ok(new BalanceGeneralDto(
            fechaCorte, activo, pasivo, patrimonio, resultadoEjercicio, resultadosAnteriores,
            totalPatrimonio, totalPasivoPatrimonio, diferencia == 0, diferencia));
    }

    public async Task<DashboardDto> DashboardAsync(int idEmpresa, CancellationToken cancellationToken)
    {
        var hoy = DateOnly.FromDateTime(_reloj.GetLocalNow().DateTime);
        var inicioMes = new DateOnly(hoy.Year, hoy.Month, 1);
        var finMes = inicioMes.AddMonths(1).AddDays(-1);
        var inicioAnio = new DateOnly(hoy.Year, 1, 1);

        var periodo = await _db.PeriodosFiscales.AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdEmpresa == idEmpresa && p.FechaInicio <= hoy && p.FechaFin >= hoy, cancellationToken);

        var asientosMes = await _db.AsientosEncabezados.CountAsync(a => a.IdEmpresa == idEmpresa
            && a.Estado == ServicioAsientos.Publicado && a.FechaAsiento >= inicioMes && a.FechaAsiento <= finMes, cancellationToken);
        var borradores = await _db.AsientosEncabezados.CountAsync(a => a.IdEmpresa == idEmpresa
            && a.Estado == ServicioAsientos.Borrador, cancellationToken);

        // Saldos de los grupos del PUC: 11 disponible, 13 deudores, 22 proveedores y 23 cuentas por pagar.
        var saldosHoy = await _calculadora.SaldosAsync(idEmpresa, null, hoy, cancellationToken);
        decimal SaldoGrupos(params string[] codigos) => saldosHoy.Where(s => codigos.Contains(s.Cuenta.Codigo)).Sum(s => s.SaldoFinal);

        // Ingresos y gastos (con costos) del año, mes a mes.
        var lineasAnio = await _calculadora.LineasPublicadas(idEmpresa, inicioAnio, new DateOnly(hoy.Year, 12, 31))
            .Where(d => TiposResultado.Contains(d.IdCuentaNavigation.TipoCuenta))
            .Select(d => new
            {
                d.IdAsientoNavigation.FechaAsiento,
                d.IdCuentaNavigation.TipoCuenta,
                Debe = d.Debe ?? 0,
                Haber = d.Haber ?? 0,
            })
            .ToListAsync(cancellationToken);

        var meses = Enumerable.Range(1, 12).Select(mes =>
        {
            var delMes = lineasAnio.Where(l => l.FechaAsiento.Month == mes).ToList();
            return new PuntoMensualDto(
                mes,
                delMes.Where(l => l.TipoCuenta == "INGRESO").Sum(l => l.Haber - l.Debe),
                delMes.Where(l => l.TipoCuenta != "INGRESO").Sum(l => l.Debe - l.Haber));
        }).ToList();

        var ingresosAnio = meses.Sum(m => m.Ingresos);
        var gastosAnio = meses.Sum(m => m.Gastos);

        var ultimos = (await _asientos.ListarAsync(idEmpresa, new FiltroAsientos(), cancellationToken)).Take(5).ToList();

        return new DashboardDto(
            hoy,
            periodo is null ? null : ServicioPeriodos.Nombre(periodo.Anio, periodo.Mes),
            periodo?.Estado,
            asientosMes,
            borradores,
            SaldoGrupos("11"),
            SaldoGrupos("13"),
            SaldoGrupos("22", "23"),
            ingresosAnio,
            gastosAnio,
            ingresosAnio - gastosAnio,
            meses,
            ultimos);
    }

    /// <summary>
    /// Cuentas de una sección con su valor en el sentido normal de la sección (por ejemplo, el activo como débito menos crédito):
    /// así una cuenta correctora, como la depreciación acumulada, aparece en negativo y resta del total.
    /// </summary>
    private static SeccionResultadosDto Seccion(
        string nombre, IEnumerable<SaldoCuenta> saldos, int nivelMaximo, Func<SaldoCuenta, bool> pertenece, Func<SaldoCuenta, decimal> valor)
    {
        var deLaSeccion = saldos.Where(pertenece).ToList();

        var filas = deLaSeccion
            .Where(s => s.Cuenta.Nivel <= nivelMaximo)
            .Select(s => new FilaValorDto(s.Cuenta.IdCuenta, s.Cuenta.Codigo, s.Cuenta.NombreCuenta, s.Cuenta.Nivel, s.Cuenta.EsMovimiento == true, valor(s)))
            .ToList();

        // El total sale de las auxiliares para no contar dos veces lo acumulado en las cuentas mayores.
        var total = deLaSeccion.Where(s => s.Cuenta.EsMovimiento == true).Sum(valor);
        return new SeccionResultadosDto(nombre, filas, total);
    }

    /// <summary>Ingresos menos costos y gastos (crédito menos débito de las cuentas de resultado).</summary>
    private static decimal Resultado(Dictionary<int, (decimal Debe, decimal Haber)> movimientos, Dictionary<int, string> tipoPorCuenta) =>
        movimientos
            .Where(m => tipoPorCuenta.TryGetValue(m.Key, out var tipo) && TiposResultado.Contains(tipo))
            .Sum(m => m.Value.Haber - m.Value.Debe);

    private static FilaCuentaDto AFila(SaldoCuenta s) => new(
        s.Cuenta.IdCuenta, s.Cuenta.Codigo, s.Cuenta.NombreCuenta, s.Cuenta.Nivel, s.Cuenta.TipoCuenta, s.Cuenta.Naturaleza,
        s.Cuenta.EsMovimiento == true, s.SaldoInicial, s.Debe, s.Haber, s.SaldoFinal);

    private static Resultado<T> RangoInvalido<T>() =>
        Resultado<T>.Fallo(TipoError.Validacion, "La fecha inicial no puede ser posterior a la fecha final.");
}
