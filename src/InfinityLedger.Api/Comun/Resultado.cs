namespace InfinityLedger.Api.Comun;

public enum TipoError
{
    /// <summary>Los datos no cumplen una regla de negocio (400).</summary>
    Validacion,

    /// <summary>El registro no existe o no pertenece a la empresa del usuario (404).</summary>
    NoEncontrado,

    /// <summary>Choca con un registro existente, p. ej. un código repetido (409).</summary>
    Conflicto,
}

/// <summary>Resultado de una operación de negocio: un valor o un error con mensaje para el usuario.</summary>
public sealed class Resultado<T>
{
    private Resultado(T? valor, TipoError? error, string? mensaje)
    {
        Valor = valor;
        Error = error;
        Mensaje = mensaje;
    }

    public T? Valor { get; }

    public TipoError? Error { get; }

    public string? Mensaje { get; }

    public bool EsExito => Error is null;

    public static Resultado<T> Ok(T valor) => new(valor, null, null);

    public static Resultado<T> Fallo(TipoError error, string mensaje) => new(default, error, mensaje);
}
