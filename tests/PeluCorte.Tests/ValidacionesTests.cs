using PeluCorte.Services;
using Xunit;

namespace PeluCorte.Tests;

public class ValidacionesTests
{
    [Theory]
    [InlineData("612345678", "612345678")]
    [InlineData("712345678", "712345678")]
    [InlineData("812345678", "812345678")]
    [InlineData("912345678", "912345678")]
    [InlineData("612 345 678", "612345678")]
    [InlineData("612-345-678", "612345678")]
    [InlineData("(612) 345 678", "612345678")]
    [InlineData("+34 612 345 678", "612345678")]
    [InlineData("+34612345678", "612345678")]
    [InlineData("0034 612345678", "612345678")]
    [InlineData("34612345678", "612345678")]
    public void Telefonos_validos_se_normalizan_a_9_digitos(string entrada, string esperado)
    {
        var result = Validaciones.NormalizarTelefonoEs(entrada);
        Assert.Equal(esperado, result);
    }

    [Theory]
    [InlineData("123456789")]   // empieza por 1, no es válido en España
    [InlineData("212345678")]   // empieza por 2
    [InlineData("512345678")]   // empieza por 5
    [InlineData("61234567")]    // 8 dígitos
    [InlineData("6123456789")]  // 10 dígitos
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc def ghi")]
    [InlineData("000000000")]
    [InlineData("612-345")]
    public void Telefonos_invalidos_devuelven_null(string entrada)
    {
        Assert.Null(Validaciones.NormalizarTelefonoEs(entrada));
    }

    [Fact]
    public void FormatearTelefono_anyade_espacios()
    {
        Assert.Equal("612 345 678", Validaciones.FormatearTelefono("612345678"));
    }

    [Theory]
    [InlineData("612345678", "móvil")]
    [InlineData("712345678", "móvil")]
    [InlineData("812345678", "fijo")]
    [InlineData("912345678", "fijo")]
    public void TipoTelefono_distingue_movil_y_fijo(string num, string tipo)
    {
        Assert.Equal(tipo, Validaciones.TipoTelefonoEs(num));
    }

    [Theory]
    [InlineData("usuario@ejemplo.com")]
    [InlineData("a.b+c@dominio.es")]
    [InlineData("test_123@sub.dominio.co")]
    public void Emails_validos_pasan(string email)
    {
        Assert.True(Validaciones.EsEmailValido(email));
    }

    [Theory]
    [InlineData("sin-arroba")]
    [InlineData("@sin-usuario.com")]
    [InlineData("usuario@")]
    [InlineData("usuario@dominio")]
    [InlineData("usuario@.com")]
    [InlineData("")]
    [InlineData(null)]
    public void Emails_invalidos_se_rechazan(string? email)
    {
        Assert.False(Validaciones.EsEmailValido(email));
    }
}
