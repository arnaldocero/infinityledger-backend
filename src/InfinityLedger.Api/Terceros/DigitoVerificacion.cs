namespace InfinityLedger.Api.Terceros;

/// <summary>Dígito de verificación (DV) del NIT según el algoritmo de la DIAN (módulo 11).</summary>
public static class DigitoVerificacion
{
    // Pesos de la DIAN, aplicados desde el dígito de la derecha hacia la izquierda.
    private static readonly int[] Pesos = [3, 7, 13, 17, 19, 23, 29, 37, 41, 43, 47, 53, 59, 67, 71];

    /// <summary>Calcula el DV de un NIT de solo dígitos (máximo 15).</summary>
    public static int Calcular(string nit)
    {
        if (nit.Length is 0 or > 15 || !nit.All(char.IsAsciiDigit))
        {
            throw new ArgumentException("El NIT debe tener entre 1 y 15 dígitos.", nameof(nit));
        }

        var suma = 0;
        for (var i = 0; i < nit.Length; i++)
        {
            suma += (nit[nit.Length - 1 - i] - '0') * Pesos[i];
        }

        var residuo = suma % 11;
        return residuo is 0 or 1 ? residuo : 11 - residuo;
    }
}
