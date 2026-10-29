using LinkShortener.Api.Application;

namespace LinkShortener.Tests.Unit;

public class TargetUrlRulesTests
{
    [Theory]
    [InlineData("http://exemplo.com/pagina")]
    [InlineData("https://exemplo.com/pagina?a=1")]
    public void Aceita_http_e_https(string url)
    {
        Assert.True(TargetUrlRules.TryValidate(url, out var uri, out _));
        Assert.Equal(url, uri.OriginalString);
    }

    [Theory]
    [InlineData("ftp://exemplo.com/arquivo")]
    [InlineData("javascript:alert(1)")]
    [InlineData("file:///c:/segredo.txt")]
    [InlineData("exemplo.com")]
    [InlineData("não é url")]
    public void Recusa_esquema_invalido(string url)
    {
        Assert.False(TargetUrlRules.TryValidate(url, out _, out var error));
        Assert.Equal("A URL de destino precisa ser um endereço http ou https válido.", error);
    }

    [Fact]
    public void Aceita_URL_com_exatamente_2048_caracteres()
    {
        var url = UrlWithLength(2048);

        Assert.True(TargetUrlRules.TryValidate(url, out _, out _));
    }

    [Fact]
    public void Recusa_URL_com_mais_de_2048_caracteres()
    {
        var url = UrlWithLength(2049);

        Assert.False(TargetUrlRules.TryValidate(url, out _, out var error));
        Assert.Equal("A URL de destino não pode ter mais de 2048 caracteres.", error);
    }

    private static string UrlWithLength(int length)
    {
        const string prefix = "https://exemplo.com/";
        return prefix + new string('a', length - prefix.Length);
    }
}
