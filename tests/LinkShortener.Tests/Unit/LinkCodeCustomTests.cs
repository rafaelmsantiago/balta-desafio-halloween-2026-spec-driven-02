using LinkShortener.Api.Application;

namespace LinkShortener.Tests.Unit;

public class LinkCodeCustomTests
{
    private const string LengthError = "O código precisa ter de 4 a 20 caracteres.";
    private const string AlphabetError =
        "O código só pode ter letras de A a Z e números de 1 a 9, sem espaços nem caracteres especiais.";

    [Theory]
    [InlineData("AB12")]
    [InlineData("ABCDEFGHIJ123456789K")]
    public void Aceita_4_e_20_caracteres(string code)
    {
        Assert.True(LinkCode.TryNormalizeCustom(code, out var normalized, out _));
        Assert.Equal(code, normalized);
    }

    [Theory]
    [InlineData("AB1")]
    [InlineData("ABCDEFGHIJ123456789KL")]
    [InlineData("")]
    public void Recusa_3_e_21_caracteres(string code)
    {
        Assert.False(LinkCode.TryNormalizeCustom(code, out _, out var error));
        Assert.Equal(LengthError, error);
    }

    [Theory]
    [InlineData("LINK0")]
    [InlineData("MEU LINK")]
    [InlineData("MEU-LINK")]
    [InlineData("MEU_LINK")]
    [InlineData("LINK!")]
    [InlineData("AÇÃO1")]
    public void Recusa_digito_0_espaco_e_caracteres_especiais(string code)
    {
        Assert.False(LinkCode.TryNormalizeCustom(code, out _, out var error));
        Assert.Equal(AlphabetError, error);
    }

    [Fact]
    public void Aceita_minusculas_e_devolve_em_maiusculas()
    {
        Assert.True(LinkCode.TryNormalizeCustom("meuLink9", out var normalized, out _));
        Assert.Equal("MEULINK9", normalized);
    }
}
