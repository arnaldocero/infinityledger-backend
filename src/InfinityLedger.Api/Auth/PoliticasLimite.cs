namespace InfinityLedger.Api.Auth;

/// <summary>Nombres de las políticas de rate limiting registradas en Program.cs.</summary>
public static class PoliticasLimite
{
    /// <summary>Máximo de intentos de login por IP y por minuto.</summary>
    public const string Login = "login";
}
