using System.Globalization;
using LinkShortener.Api.Application;

namespace LinkShortener.Tests.Unit;

public class LinkCodeNormalizeTests
{
    [Theory]
    [InlineData("abc123", "ABC123")]
    [InlineData("MeuLink", "MEULINK")]
    [InlineData("JA9XQ2B", "JA9XQ2B")]
    public void Normalize_converte_para_maiusculas(string input, string expected)
    {
        Assert.Equal(expected, LinkCode.Normalize(input));
    }

    [Fact]
    public void Normalize_nao_depende_da_cultura_turca()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");

            Assert.Equal("LINK1", LinkCode.Normalize("link1"));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
