using System.Net;
using System.Net.Http.Json;
using LinkShortener.Api.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace LinkShortener.Tests.Integration;

public class AccessCountTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();
    private readonly LinkShortenerFactory _factory;
    private readonly HttpClient _client;

    public AccessCountTests()
    {
        _factory = new LinkShortenerFactory(_database.ConnectionString);
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task Acessos_repetidos_incrementam_o_contador_a_cada_requisicao()
    {
        await CreateAsync("PROMO26");

        for (var i = 0; i < 3; i++)
            Assert.Equal(HttpStatusCode.Found, (await _client.GetAsync("/PROMO26")).StatusCode);

        Assert.Equal(3, AccessCountOf("PROMO26"));
    }

    [Fact]
    public async Task Acessos_concorrentes_nao_perdem_contagem()
    {
        const int requests = 50;
        await CreateAsync("PROMO26");

        var responses = await Task.WhenAll(
            Enumerable.Range(0, requests).Select(_ => _client.GetAsync("/PROMO26")));

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.Found, r.StatusCode));
        Assert.Equal(requests, AccessCountOf("PROMO26"));
    }

    private async Task CreateAsync(string code)
    {
        var response = await _client.PostAsJsonAsync("/api/links", new { targetUrl = "https://exemplo.com/", code });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private long AccessCountOf(string code)
    {
        using var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<LinksDbContext>().Links.Single(l => l.Code == code).AccessCount;
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _database.Dispose();
    }
}
