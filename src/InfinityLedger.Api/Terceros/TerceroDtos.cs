using System.ComponentModel.DataAnnotations;

namespace InfinityLedger.Api.Terceros;

/// <summary>Tercero tal como lo usa el frontend. TiposTercero viene de la columna SET tipo_tercero.</summary>
public record TerceroDto(
    int IdTercero,
    string TipoDocumento,
    string NumeroDocumento,
    int? DigitoVerificacion,
    string NombreRazonSocial,
    IReadOnlyList<string> TiposTercero,
    string? Direccion,
    string? Telefono,
    string? Email,
    bool Activo);

public record GuardarTerceroRequest
{
    [Required]
    public string TipoDocumento { get; init; } = "";

    [Required, MaxLength(20)]
    public string NumeroDocumento { get; init; } = "";

    /// <summary>Opcional para NIT/RUT: si se envía debe coincidir con el calculado.</summary>
    [Range(0, 9)]
    public int? DigitoVerificacion { get; init; }

    [Required, MinLength(3), MaxLength(200)]
    public string NombreRazonSocial { get; init; } = "";

    [Required, MinLength(1)]
    public IReadOnlyList<string> TiposTercero { get; init; } = [];

    [MaxLength(200)]
    public string? Direccion { get; init; }

    [MaxLength(30)]
    public string? Telefono { get; init; }

    [EmailAddress, MaxLength(100)]
    public string? Email { get; init; }

    public bool Activo { get; init; } = true;
}
