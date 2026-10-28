namespace InfinityLedger.Api.Auth;

/// <summary>Sección "Jwt" de la configuración. La clave va en user-secrets o en variables de entorno, nunca en appsettings.</summary>
public class JwtOpciones
{
    public const string Seccion = "Jwt";

    public string Emisor { get; set; } = "InfinityLedger.Api";

    public string Audiencia { get; set; } = "InfinityLedger.Frontend";

    /// <summary>Clave HMAC-SHA256 en base64, de al menos 32 bytes.</summary>
    public string Clave { get; set; } = "";

    public int ExpiracionMinutos { get; set; } = 480;
}
