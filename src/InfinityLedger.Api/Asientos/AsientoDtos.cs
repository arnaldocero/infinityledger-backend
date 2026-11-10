using System.ComponentModel.DataAnnotations;

namespace InfinityLedger.Api.Asientos;

/// <summary>Fila del listado de asientos, con los totales de sus líneas.</summary>
public record AsientoResumenDto(
    int IdAsiento,
    int IdTipoComprobante,
    string Comprobante,
    int NumeroComprobante,
    DateOnly FechaAsiento,
    int IdPeriodo,
    string ConceptoGeneral,
    string Estado,
    decimal TotalDebe,
    decimal TotalHaber);

public record LineaAsientoDto(
    int IdAsientoDetalle,
    int IdCuenta,
    string CodigoCuenta,
    string NombreCuenta,
    int? IdTercero,
    string? NombreTercero,
    int? IdCentroCosto,
    string? CodigoCentroCosto,
    string? Concepto,
    decimal Debe,
    decimal Haber);

public record AsientoDto(
    int IdAsiento,
    int IdTipoComprobante,
    string Comprobante,
    int NumeroComprobante,
    DateOnly FechaAsiento,
    int IdPeriodo,
    string ConceptoGeneral,
    string Estado,
    decimal TotalDebe,
    decimal TotalHaber,
    IReadOnlyList<LineaAsientoDto> Lineas);

public record LineaAsientoRequest
{
    [Range(1, int.MaxValue)]
    public int IdCuenta { get; init; }

    public int? IdTercero { get; init; }

    public int? IdCentroCosto { get; init; }

    [MaxLength(255)]
    public string? Concepto { get; init; }

    [Range(typeof(decimal), "0", "9999999999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal Debe { get; init; }

    [Range(typeof(decimal), "0", "9999999999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal Haber { get; init; }
}

public record GuardarAsientoRequest
{
    [Range(1, int.MaxValue)]
    public int IdTipoComprobante { get; init; }

    public DateOnly FechaAsiento { get; init; }

    [Required, MinLength(3), MaxLength(1000)]
    public string ConceptoGeneral { get; init; } = "";

    [Required, MinLength(1), MaxLength(500)]
    public IReadOnlyList<LineaAsientoRequest> Lineas { get; init; } = [];

    /// <summary>true: se publica al guardar (exige cuadre). false: queda en borrador.</summary>
    public bool Publicar { get; init; }
}

public record FiltroAsientos
{
    public int? IdPeriodo { get; init; }

    public string? Estado { get; init; }

    public int? IdTipoComprobante { get; init; }
}
