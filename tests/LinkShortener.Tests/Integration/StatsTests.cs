using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LinkShortener.Tests.Integration;

public class StatsTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();
    private readonly LinkShortenerFactory _factory;
    private readonly HttpClient _client;

    public StatsTests()
    {
        _factory = new LinkShortenerFactory(_database.ConnectionString);
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    private sealed record Stats(string Code, long AccessCount);

    private sealed record CreatedLink(string ShortUrl, string Code, string? ExpiresOn);

    [Theory]
    [InlineData("PROMO26")]
    [InlineData("promo26")]
    public async Task Retorna_200_com_o_total_de_acessos(string code)
    {
        await CreateAsync(new { targetUrl = "https://exemplo.com/", code = "PROMO26" });
        await _client.GetAsync("/PROMO26");
        await _client.GetAsync("/PROMO26");

        var response = await _client.GetAsync($"/api/links/{code}/stats");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new Stats("PROMO26", 2), await response.Content.ReadFromJsonAsync<Stats>());
    }

    [Fact]
    public async Task Retorna_o_total_de_link_expirado()
    {
        await CreateAsync(new { targetUrl = "https://exemplo.com/", code = "PROMO26", expiresOn = "2026-10-29" });
        await _client.GetAsync("/PROMO26");
        _factory.Clock.UtcNow = _factory.Clock.UtcNow.AddDays(5);

        var response = await _client.GetAsync("/api/links/PROMO26/stats");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, (await response.Content.ReadFromJsonAsync<Stats>())!.AccessCount);
    }

    [Fact]
    public async Task Mesma_URL_encurtada_duas_vezes_tem_contadores_independentes()
    {
        var first = await CreateAsync(new { targetUrl = "https://exemplo.com/mesma" });
        var second = await CreateAsync(new { targetUrl = "https://exemplo.com/mesma" });
        await _client.GetAsync($"/{first.Code}");

        Assert.Equal(1, (await _client.GetFromJsonAsync<Stats>($"/api/links/{first.Code}/stats"))!.AccessCount);
        Assert.Equal(0, (await _client.GetFromJsonAsync<Stats>($"/api/links/{second.Code}/stats"))!.AccessCount);
    }

    [Fact]
    public async Task Codigo_inexistente_retorna_404_com_ProblemDetails()
    {
        var response = await _client.GetAsync("/api/links/NAOEXISTE/stats");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("O link informado não existe.", problem!.Detail);
    }

    private async Task<CreatedLink> CreateAsync(object request)
    {
        var response = await _client.PostAsJsonAsync("/api/links", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CreatedLink>())!;
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _database.Dispose();
    }
}
