using System.ComponentModel.DataAnnotations;

namespace InfinityLedger.Api.CentrosCosto;

public record CentroCostoDto(int IdCentroCosto, string Codigo, string Nombre, bool Activo);

public record GuardarCentroCostoRequest
{
    [Required, MaxLength(20)]
    [RegularExpression(@"^[A-Za-z0-9-]+$", ErrorMessage = "El código solo puede tener letras, dígitos y guiones.")]
    public string Codigo { get; init; } = "";

    [Required, MinLength(3), MaxLength(100)]
    public string Nombre { get; init; } = "";

    public bool Activo { get; init; } = true;
}
