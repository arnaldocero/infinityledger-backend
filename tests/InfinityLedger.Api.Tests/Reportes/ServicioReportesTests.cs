using InfinityLedger.Api.Asientos;
using InfinityLedger.Api.Comun;
using InfinityLedger.Api.Data;
using InfinityLedger.Api.Data.Entities;
using InfinityLedger.Api.Periodos;
using InfinityLedger.Api.Reportes;
using InfinityLedger.Api.Tests.Infraestructura;
using Microsoft.EntityFrameworkCore;

namespace InfinityLedger.Api.Tests.Reportes;

/// <summary>
/// Escenario (empresa 1), además del seed:
///   2025-12-20  Caja 300.000 a Ventas                      (año anterior)
///   2026-09-01  Caja 10.000.000 a Capital                   (aporte)
///   2026-10-05  Caja 1.190.000 a Ventas                     (venta)
///   2026-10-10  Servicios 200.000 a Proveedores             (gasto a crédito)
///   2026-10-12  BORRADOR y 2026-10-15 ANULADO: no deben contar en ningún reporte.
/// </summary>
public class ServicioReportesTests
{
    private const int Capital = 50;
    private const int Proveedores = 60;
    private const int GrupoProveedores = 61;

    private static readonly DateOnly InicioOctubre = new(2026, 10, 1);
    private static readonly DateOnly FinOctubre = new(2026, 10, 31);

    [Fact]
    public async Task Balance_de_comprobacion_de_octubre_cuadra_y_trae_el_saldo_anterior()
    {
        await using var db = CrearEscenario();

        var balance = (await Servicio(db).BalanceComprobacionAsync(1, InicioOctubre, FinOctubre, 4, CancellationToken.None)).Valor!;

        var caja = balance.Filas.Single(f => f.Codigo == "110505");
        Assert.Equal((10_300_000m, 1_190_000m, 0m, 11_490_000m), (caja.SaldoInicial, caja.Debitos, caja.Creditos, caja.SaldoFinal));
        Assert.Equal(11_490_000m, balance.Filas.Single(f => f.Codigo == "1").SaldoFinal); // acumulado en la clase
        Assert.Equal((1_390_000m, 1_390_000m, true), (balance.TotalDebitos, balance.TotalCreditos, balance.Cuadra));
    }

    [Fact]
    public async Task Balance_de_comprobacion_respeta_el_nivel_maximo()
    {
        await using var db = CrearEscenario();

        var balance = (await Servicio(db).BalanceComprobacionAsync(1, InicioOctubre, FinOctubre, 2, CancellationToken.None)).Valor!;

        Assert.All(balance.Filas, f => Assert.True(f.Nivel <= 2));
        Assert.Contains(balance.Filas, f => f.Codigo == "11");
    }

    [Fact]
    public async Task Estado_de_resultados_de_octubre()
    {
        await using var db = CrearEscenario();

        var estado = (await Servicio(db).EstadoResultadosAsync(1, InicioOctubre, FinOctubre, 4, CancellationToken.None)).Valor!;

        Assert.Equal((1_190_000m, 0m, 200_000m), (estado.Ingresos.Total, estado.Costos.Total, estado.Gastos.Total));
        Assert.Equal((1_190_000m, 990_000m), (estado.UtilidadBruta, estado.UtilidadNeta));
        Assert.Contains(estado.Ingresos.Filas, f => f.Codigo == "413505" && f.Valor == 1_190_000m);
    }

    [Fact]
    public async Task Balance_general_cuadra_con_la_utilidad_del_ejercicio_y_la_de_anios_anteriores()
    {
        await using var db = CrearEscenario();

        var balance = (await Servicio(db).BalanceGeneralAsync(1, FinOctubre, 4, CancellationToken.None)).Valor!;

        Assert.Equal(11_490_000m, balance.Activo.Total);
        Assert.Equal(200_000m, balance.Pasivo.Total);
        Assert.Equal(10_000_000m, balance.Patrimonio.Total);
        Assert.Equal((990_000m, 300_000m), (balance.ResultadoEjercicio, balance.ResultadosAnteriores));
        Assert.Equal((11_290_000m, 11_490_000m), (balance.TotalPatrimonio, balance.TotalPasivoPatrimonio));
        Assert.True(balance.Cuadra);
    }

    [Fact]
    public async Task Libro_diario_solo_trae_asientos_publicados_del_rango()
    {
        await using var db = CrearEscenario();

        var diario = (await Servicio(db).LibroDiarioAsync(1, InicioOctubre, FinOctubre, CancellationToken.None)).Valor!;

        Assert.Equal(new[] { "ING-0010", "ING-0011" }, diario.Asientos.Select(a => a.Comprobante));
        Assert.Equal((1_390_000m, 1_390_000m), (diario.TotalDebe, diario.TotalHaber));
    }

    [Fact]
    public async Task Libro_mayor_de_una_auxiliar_lleva_el_saldo_acumulado()
    {
        await using var db = CrearEscenario();

        var mayor = (await Servicio(db).LibroMayorAsync(1, DatosDePrueba.CajaGeneral, InicioOctubre, FinOctubre, CancellationToken.None)).Valor!;

        Assert.Equal(10_300_000m, mayor.SaldoInicial);
        var movimiento = Assert.Single(mayor.Movimientos);
        Assert.Equal(("ING-0010", 1_190_000m, 11_490_000m), (movimiento.Comprobante, movimiento.Debe, movimiento.Saldo));
        Assert.Equal(11_490_000m, mayor.SaldoFinal);
    }

    [Fact]
    public async Task Libro_mayor_de_una_cuenta_mayor_incluye_sus_subcuentas()
    {
        await using var db = CrearEscenario();

        var mayor = (await Servicio(db).LibroMayorAsync(1, DatosDePrueba.Caja, InicioOctubre, FinOctubre, CancellationToken.None)).Valor!;

        Assert.Equal("110505", Assert.Single(mayor.Movimientos).CodigoCuenta);
        Assert.Equal(11_490_000m, mayor.SaldoFinal);
    }

    [Fact]
    public async Task Libro_mayor_de_una_cuenta_credito_suma_los_creditos()
    {
        await using var db = CrearEscenario();

        var mayor = (await Servicio(db).LibroMayorAsync(1, DatosDePrueba.Ventas, InicioOctubre, FinOctubre, CancellationToken.None)).Valor!;

        Assert.Equal((300_000m, 1_490_000m), (mayor.SaldoInicial, mayor.SaldoFinal));
        Assert.Equal("Cliente Demo S.A.S.", mayor.Movimientos.Single().NombreTercero);
    }

    [Fact]
    public async Task Rango_invertido_y_cuenta_ajena_fallan()
    {
        await using var db = CrearEscenario();
        var servicio = Servicio(db);

        var rango = await servicio.BalanceComprobacionAsync(1, FinOctubre, InicioOctubre, 4, CancellationToken.None);
        var ajena = await servicio.LibroMayorAsync(1, DatosDePrueba.ActivoEmpresa2, InicioOctubre, FinOctubre, CancellationToken.None);

        Assert.Equal(TipoError.Validacion, rango.Error);
        Assert.Equal(TipoError.NoEncontrado, ajena.Error);
    }

    [Fact]
    public async Task Saldos_mensuales_de_un_periodo_abierto_se_calculan_al_momento()
    {
        await using var db = CrearEscenario();

        var saldos = (await Servicio(db).SaldosMensualesAsync(1, DatosDePrueba.Octubre2026, CancellationToken.None)).Valor!;

        Assert.False(saldos.Definitivo);
        Assert.All(saldos.Filas, f => Assert.True(f.EsMovimiento));
        Assert.Equal(11_490_000m, saldos.Filas.Single(f => f.Codigo == "110505").SaldoFinal);
        Assert.Equal(10_000_000m, saldos.Filas.Single(f => f.IdCuenta == Capital).SaldoInicial); // sin movimiento en octubre
    }

    [Fact]
    public async Task Al_cerrar_un_periodo_se_guardan_sus_saldos_y_al_reabrirlo_se_borran()
    {
        await using var db = CrearEscenario();
        var periodos = new ServicioPeriodos(db);
        var admin = new UsuarioActual(1, 1, null);

        await periodos.ReabrirAsync(admin, DatosDePrueba.Agosto2026, CancellationToken.None);
        await periodos.CerrarAsync(admin, DatosDePrueba.Agosto2026, CancellationToken.None);
        var cierre = await periodos.CerrarAsync(admin, DatosDePrueba.Septiembre2026, CancellationToken.None);
        var guardados = (await Servicio(db).SaldosMensualesAsync(1, DatosDePrueba.Septiembre2026, CancellationToken.None)).Valor!;

        Assert.True(cierre.EsExito, cierre.Mensaje);
        Assert.True(guardados.Definitivo);
        var caja = db.SaldosMensuales.Single(s => s.IdPeriodo == DatosDePrueba.Septiembre2026 && s.IdCuenta == DatosDePrueba.CajaGeneral);
        Assert.Equal((300_000m, 10_000_000m, 10_300_000m), (caja.SaldoInicial, caja.TotalDebe, caja.SaldoFinal));
        Assert.Equal(10_300_000m, guardados.Filas.Single(f => f.Codigo == "110505").SaldoFinal);

        await periodos.ReabrirAsync(admin, DatosDePrueba.Septiembre2026, CancellationToken.None);
        Assert.Empty(db.SaldosMensuales.Where(s => s.IdPeriodo == DatosDePrueba.Septiembre2026));
    }

    [Fact]
    public async Task Dashboard_resume_el_mes_y_el_anio()
    {
        await using var db = CrearEscenario();

        var dashboard = await Servicio(db, hoy: new DateTimeOffset(2026, 10, 20, 12, 0, 0, TimeSpan.Zero)).DashboardAsync(1, CancellationToken.None);

        Assert.Equal(("octubre 2026", "ABIERTO"), (dashboard.Periodo, dashboard.EstadoPeriodo));
        Assert.Equal((2, 2), (dashboard.AsientosPublicadosMes, dashboard.Borradores));
        Assert.Equal((11_490_000m, 200_000m), (dashboard.Efectivo, dashboard.CuentasPorPagar));
        Assert.Equal((1_190_000m, 200_000m, 990_000m), (dashboard.IngresosAnio, dashboard.GastosAnio, dashboard.UtilidadAnio));
        Assert.Equal((1_190_000m, 200_000m), (dashboard.Meses[9].Ingresos, dashboard.Meses[9].Gastos));
        Assert.Equal(12, dashboard.Meses.Count);
    }

    private static ServicioReportes Servicio(CodeLedgerContext db, DateTimeOffset? hoy = null) =>
        new(db, new ServicioAsientos(db), new RelojFijo(hoy ?? new DateTimeOffset(2026, 10, 20, 12, 0, 0, TimeSpan.Zero)));

    private static CodeLedgerContext CrearEscenario()
    {
        var db = new CodeLedgerContext(new DbContextOptionsBuilder<CodeLedgerContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        DatosDePrueba.Cargar(db);

        // Pasivo y patrimonio para el balance general.
        db.PlanCuentas.AddRange(
            Cuenta(GrupoProveedores, "22", "PROVEEDORES", DatosDePrueba.Pasivo, 2, "PASIVO", "CREDITO", false),
            Cuenta(62, "2205", "NACIONALES", GrupoProveedores, 3, "PASIVO", "CREDITO", false),
            Cuenta(Proveedores, "220505", "Proveedores nacionales", 62, 4, "PASIVO", "CREDITO", true),
            Cuenta(51, "3", "PATRIMONIO", null, 1, "PATRIMONIO", "CREDITO", false),
            Cuenta(52, "31", "CAPITAL SOCIAL", 51, 2, "PATRIMONIO", "CREDITO", false),
            Cuenta(53, "3105", "CAPITAL SUSCRITO", 52, 3, "PATRIMONIO", "CREDITO", false),
            Cuenta(Capital, "310505", "Capital suscrito y pagado", 53, 4, "PATRIMONIO", "CREDITO", true));

        Asiento(db, 20, 9, new DateOnly(2025, 12, 20), "PUBLICADO", (DatosDePrueba.CajaGeneral, 300_000, 0, null), (DatosDePrueba.Ventas, 0, 300_000, DatosDePrueba.ClienteDemo));
        Asiento(db, 21, 8, new DateOnly(2026, 9, 1), "PUBLICADO", (DatosDePrueba.CajaGeneral, 10_000_000, 0, null), (Capital, 0, 10_000_000, null));
        Asiento(db, 22, 10, new DateOnly(2026, 10, 5), "PUBLICADO", (DatosDePrueba.CajaGeneral, 1_190_000, 0, null), (DatosDePrueba.Ventas, 0, 1_190_000, DatosDePrueba.ClienteDemo));
        Asiento(db, 23, 11, new DateOnly(2026, 10, 10), "PUBLICADO", (DatosDePrueba.Servicios, 200_000, 0, null), (Proveedores, 0, 200_000, null));
        Asiento(db, 24, 12, new DateOnly(2026, 10, 12), "BORRADOR", (DatosDePrueba.CajaGeneral, 999, 0, null), (DatosDePrueba.CajaGeneral, 0, 999, null));
        Asiento(db, 25, 13, new DateOnly(2026, 10, 15), "ANULADO", (DatosDePrueba.CajaGeneral, 5_000, 0, null), (DatosDePrueba.Ventas, 0, 5_000, DatosDePrueba.ClienteDemo));

        db.SaveChanges();
        return db;
    }

    private static void Asiento(CodeLedgerContext db, int id, int numero, DateOnly fecha, string estado,
        params (int Cuenta, decimal Debe, decimal Haber, int? Tercero)[] lineas)
    {
        db.AsientosEncabezados.Add(new AsientoEncabezado
        {
            IdAsiento = id,
            IdEmpresa = 1,
            IdTipoComprobante = DatosDePrueba.TipoIngreso,
            NumeroComprobante = numero,
            IdPeriodo = fecha.Month switch { 9 => DatosDePrueba.Septiembre2026, 10 => DatosDePrueba.Octubre2026, _ => DatosDePrueba.Agosto2026 },
            IdUsuario = 1,
            FechaAsiento = fecha,
            ConceptoGeneral = $"Asiento {numero}",
            Estado = estado,
            AsientosDetalles = lineas.Select(l => new AsientoDetalle { IdCuenta = l.Cuenta, Debe = l.Debe, Haber = l.Haber, IdTercero = l.Tercero }).ToList(),
        });
    }

    private static PlanCuenta Cuenta(int id, string codigo, string nombre, int? padre, int nivel, string tipo, string naturaleza, bool movimiento) => new()
    {
        IdCuenta = id,
        IdEmpresa = 1,
        Codigo = codigo,
        NombreCuenta = nombre,
        IdCuentaPadre = padre,
        Nivel = nivel,
        TipoCuenta = tipo,
        Naturaleza = naturaleza,
        EsMovimiento = movimiento,
        PideTercero = false,
        PideCentroCosto = false,
        Activo = true,
    };

    private sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
