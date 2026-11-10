using System.ComponentModel.DataAnnotations;

namespace InfinityLedger.Api.Comprobantes;

/// <summary>Tipo de comprobante. ConsecutivoActual es el número que recibirá el próximo asiento de este tipo.</summary>
public record TipoComprobanteDto(int IdTipoComprobante, string Codigo, string Nombre, int ConsecutivoActual);

/// <summary>Campos editables. El código no cambia: identifica los comprobantes ya numerados.</summary>
public record EditarTipoComprobanteRequest
{
    [Required, MinLength(3), MaxLength(80)]
    public string Nombre { get; init; } = "";

    [Range(1, int.MaxValue)]
    public int ConsecutivoActual { get; init; } = 1;
}

public record CrearTipoComprobanteRequest : EditarTipoComprobanteRequest
{
    [Required]
    [RegularExpression(@"^[A-Za-z]{2,10}$", ErrorMessage = "El código debe tener de 2 a 10 letras.")]
    public string Codigo { get; init; } = "";
}
