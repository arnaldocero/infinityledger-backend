namespace InfinityLedger.Api.PlanCuentas;

/// <summary>Estructura del Plan Único de Cuentas (PUC) colombiano. Mismas reglas que cuenta.model.ts del frontend.</summary>
public static class ReglasPuc
{
    public static readonly IReadOnlySet<string> TiposCuenta = new HashSet<string>
    {
        "ACTIVO", "PASIVO", "PATRIMONIO", "INGRESO", "GASTO", "COSTO_VENTAS", "COSTO_PRODUCCION", "ORDEN",
    };

    public static readonly IReadOnlySet<string> Naturalezas = new HashSet<string> { "DEBITO", "CREDITO" };

    /// <summary>Nivel según la longitud del código: 1 clase, 2 grupo, 4 cuenta, 6 subcuenta.</summary>
    public static int? NivelPorCodigo(string codigo) => codigo.Length switch
    {
        1 => 1,
        2 => 2,
        4 => 3,
        6 => 4,
        _ => null,
    };

    /// <summary>'110505' → '1105', '1105' → '11', '11' → '1', '1' → null.</summary>
    public static string? CodigoPadre(string codigo) => codigo.Length switch
    {
        6 => codigo[..4],
        4 => codigo[..2],
        2 => codigo[..1],
        _ => null,
    };
}
