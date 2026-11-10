using System.ComponentModel.DataAnnotations;

namespace InfinityLedger.Api.Periodos;

/// <summary>Período fiscal mensual. Estado: ABIERTO o CERRADO.</summary>
public record PeriodoDto(int IdPeriodo, int Anio, int Mes, DateOnly FechaInicio, DateOnly FechaFin, string Estado);

public record CrearPeriodoRequest
{
    [Range(2000, 2100)]
    public int Anio { get; init; }

    [Range(1, 12)]
    public int Mes { get; init; }
}
