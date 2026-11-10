using InfinityLedger.Api.Terceros;

namespace InfinityLedger.Api.Tests.Terceros;

public class DigitoVerificacionTests
{
    [Theory]
    [InlineData("800197268", 4)] // DIAN
    [InlineData("860034313", 7)] // Davivienda
    [InlineData("900123456", 8)] // tercero demo del seed
    [InlineData("901345678", 2)]
    public void Calcula_el_digito_de_la_dian(string nit, int esperado)
    {
        Assert.Equal(esperado, DigitoVerificacion.Calcular(nit));
    }

    [Theory]
    [InlineData("")]
    [InlineData("90012345A")]
    [InlineData("1234567890123456")]
    public void Rechaza_nit_no_numerico_o_largo(string nit)
    {
        Assert.Throws<ArgumentException>(() => DigitoVerificacion.Calcular(nit));
    }
}
