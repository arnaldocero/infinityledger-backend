using System.ComponentModel.DataAnnotations;

namespace InfinityLedger.Api.PlanCuentas;

/// <summary>Cuenta tal como la usa el frontend (modelo CuentaContable).</summary>
public record CuentaDto(
    int IdCuenta,
    string Codigo,
    string NombreCuenta,
    int? IdCuentaPadre,
    int Nivel,
    string TipoCuenta,
    string Naturaleza,
    bool EsMovimiento,
    bool PideTercero,
    bool PideCentroCosto,
    bool Activo);

/// <summary>Campos que se pueden cambiar de una cuenta. El código y el nivel no se editan.</summary>
public record EditarCuentaRequest
{
    [Required, MinLength(3), MaxLength(150)]
    public string NombreCuenta { get; init; } = "";

    [Required]
    public string TipoCuenta { get; init; } = "";

    [Required]
    public string Naturaleza { get; init; } = "";

    public bool EsMovimiento { get; init; }

    public bool PideTercero { get; init; }

    public bool PideCentroCosto { get; init; }

    public bool Activo { get; init; } = true;
}

public record CrearCuentaRequest : EditarCuentaRequest
{
    [Required, RegularExpression(@"^\d+$", ErrorMessage = "El código solo puede tener dígitos.")]
    public string Codigo { get; init; } = "";
}
