using System.Net;
using LinkShortener.Api.Application;

namespace LinkShortener.Tests.Unit;

public class PublicHostCheckerTests
{
    [Fact]
    public async Task Recusa_host_que_nao_resolve()
    {
        Assert.False(await IsAccessible("https://nao-existe.exemplo/"));
    }

    [Theory]
    [InlineData("127.0.0.1")]       // loopback
    [InlineData("::1")]             // loopback IPv6
    [InlineData("10.1.2.3")]        // privado
    [InlineData("172.16.0.1")]      // privado
    [InlineData("192.168.0.10")]    // privado
    [InlineData("fd00::1")]         // privado IPv6
    [InlineData("169.254.169.254")] // link-local
    [InlineData("fe80::1")]         // link-local IPv6
    [InlineData("0.0.0.0")]         // reservado
    [InlineData("100.64.0.1")]      // reservado (CGNAT)
    [InlineData("192.0.2.1")]       // reservado (documentação)
    [InlineData("240.0.0.1")]       // reservado
    [InlineData("224.0.0.1")]       // multicast
    [InlineData("::")]              // não especificado
    [InlineData("::ffff:10.0.0.1")] // IPv4 privado mapeado em IPv6
    public async Task Recusa_endereco_nao_publico(string address)
    {
        Assert.False(await IsAccessible("https://destino.exemplo/", address));
    }

    [Theory]
    [InlineData("93.184.216.34")]
    [InlineData("2606:2800:220:1:248:1893:25c8:1946")]
    public async Task Aceita_host_publico(string address)
    {
        Assert.True(await IsAccessible("https://destino.exemplo/", address));
    }

    [Fact]
    public async Task Recusa_host_que_resolve_para_publico_e_privado()
    {
        Assert.False(await IsAccessible("https://destino.exemplo/", "93.184.216.34", "10.0.0.1"));
    }

    [Fact]
    public async Task Consulta_o_resolvedor_com_o_host_da_URL()
    {
        var resolver = new FakeHostResolver("93.184.216.34");
        var checker = new PublicHostChecker(resolver);

        await checker.IsPubliclyAccessibleAsync(new Uri("https://www.exemplo.com:8443/a"), CancellationToken.None);

        Assert.Equal("www.exemplo.com", resolver.LastHost);
    }

    private static Task<bool> IsAccessible(string url, params string[] addresses) =>
        new PublicHostChecker(new FakeHostResolver(addresses))
            .IsPubliclyAccessibleAsync(new Uri(url), CancellationToken.None);
}

public sealed class FakeHostResolver(params string[] addresses) : IHostResolver
{
    public string? LastHost { get; private set; }

    public Task<IReadOnlyList<IPAddress>> ResolveAsync(string host, CancellationToken cancellationToken)
    {
        LastHost = host;
        return Task.FromResult<IReadOnlyList<IPAddress>>(addresses.Select(IPAddress.Parse).ToArray());
    }
}
