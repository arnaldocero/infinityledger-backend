using InfinityLedger.Api.Asientos;

namespace InfinityLedger.Api.Reportes;

/// <summary>Fila de cuenta en los reportes de saldos. Los saldos tienen el signo de la naturaleza de la cuenta.</summary>
public record FilaCuentaDto(
    int IdCuenta,
    string Codigo,
    string NombreCuenta,
    int Nivel,
    string TipoCuenta,
    string Naturaleza,
    bool EsMovimiento,
    decimal SaldoInicial,
    decimal Debitos,
    decimal Creditos,
    decimal SaldoFinal);

// ----- Saldos mensuales -----

/// <param name="Definitivo">true: período cerrado, saldos guardados al cerrar. false: período abierto, calculados ahora.</param>
public record SaldosMensualesDto(
    int IdPeriodo,
    string Periodo,
    bool Definitivo,
    IReadOnlyList<FilaCuentaDto> Filas,
    decimal TotalDebitos,
    decimal TotalCreditos);

// ----- Libros -----

public record LibroDiarioDto(DateOnly Desde, DateOnly Hasta, IReadOnlyList<AsientoDto> Asientos, decimal TotalDebe, decimal TotalHaber);

public record MovimientoMayorDto(
    DateOnly Fecha,
    int IdAsiento,
    string Comprobante,
    string Concepto,
    string? CodigoCuenta,
    string? NombreTercero,
    decimal Debe,
    decimal Haber,
    decimal Saldo);

public record LibroMayorDto(
    int IdCuenta,
    string Codigo,
    string NombreCuenta,
    string Naturaleza,
    DateOnly Desde,
    DateOnly Hasta,
    decimal SaldoInicial,
    IReadOnlyList<MovimientoMayorDto> Movimientos,
    decimal TotalDebe,
    decimal TotalHaber,
    decimal SaldoFinal);

// ----- Balances -----

public record BalanceComprobacionDto(
    DateOnly Desde,
    DateOnly Hasta,
    int NivelMaximo,
    IReadOnlyList<FilaCuentaDto> Filas,
    decimal TotalDebitos,
    decimal TotalCreditos,
    bool Cuadra);

/// <summary>Una sección (ingresos, costos o gastos) con sus cuentas; Valor es positivo en el sentido normal de la sección.</summary>
public record SeccionResultadosDto(string Nombre, IReadOnlyList<FilaValorDto> Filas, decimal Total);

public record FilaValorDto(int IdCuenta, string Codigo, string NombreCuenta, int Nivel, bool EsMovimiento, decimal Valor);

public record EstadoResultadosDto(
    DateOnly Desde,
    DateOnly Hasta,
    SeccionResultadosDto Ingresos,
    SeccionResultadosDto Costos,
    SeccionResultadosDto Gastos,
    decimal UtilidadBruta,
    decimal UtilidadNeta);

public record BalanceGeneralDto(
    DateOnly FechaCorte,
    SeccionResultadosDto Activo,
    SeccionResultadosDto Pasivo,
    SeccionResultadosDto Patrimonio,
    decimal ResultadoEjercicio,
    decimal ResultadosAnteriores,
    decimal TotalPatrimonio,
    decimal TotalPasivoPatrimonio,
    bool Cuadra,
    decimal Diferencia);

// ----- Dashboard -----

public record PuntoMensualDto(int Mes, decimal Ingresos, decimal Gastos);

public record DashboardDto(
    DateOnly Fecha,
    string? Periodo,
    string? EstadoPeriodo,
    int AsientosPublicadosMes,
    int Borradores,
    decimal Efectivo,
    decimal CuentasPorCobrar,
    decimal CuentasPorPagar,
    decimal IngresosAnio,
    decimal GastosAnio,
    decimal UtilidadAnio,
    IReadOnlyList<PuntoMensualDto> Meses,
    IReadOnlyList<AsientoResumenDto> UltimosAsientos);
