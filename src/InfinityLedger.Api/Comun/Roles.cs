namespace InfinityLedger.Api.Comun;

/// <summary>Valores del enum usuarios.rol, para usar en [Authorize(Roles = ...)].</summary>
public static class Roles
{
    public const string Admin = "ADMIN";
    public const string Contador = "CONTADOR";
    public const string Auxiliar = "AUXILIAR";
    public const string Auditor = "AUDITOR";

    public const string AdminOContador = Admin + "," + Contador;
}
