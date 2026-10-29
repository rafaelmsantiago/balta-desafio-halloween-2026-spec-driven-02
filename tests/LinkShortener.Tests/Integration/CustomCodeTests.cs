using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace LinkShortener.Tests.Integration;

public class CustomCodeTests : IDisposable
{
    private const string CodeInUseDetail =
        "O código informado já está em uso por outro link, inclusive se ele já expirou. Escolha outro código.";

    private readonly SqliteTestDatabase _database = new();
    private readonly LinkShortenerFactory _factory;
    private readonly HttpClient _client;

    public CustomCodeTests()
    {
        _factory = new LinkShortenerFactory(_database.ConnectionString);
        _client = _factory.CreateClient();
    }

    private sealed record CreatedLink(string ShortUrl, string Code, string? ExpiresOn);

    [Fact]
    public async Task Cria_link_com_codigo_proprio_em_maiusculas()
    {
        var response = await _client.PostAsJsonAsync("/api/links", new { targetUrl = "https://exemplo.com/", code = "promo26" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreatedLink>();
        Assert.Equal("PROMO26", body!.Code);
    }

    [Fact]
    public async Task Codigo_duplicado_retorna_409_com_ProblemDetails()
    {
        await CreateAsync(new { targetUrl = "https://exemplo.com/1", code = "PROMO26" });

        var response = await _client.PostAsJsonAsync("/api/links", new { targetUrl = "https://exemplo.com/2", code = "PROMO26" });

        await AssertConflictAsync(response);
    }

    [Fact]
    public async Task Codigo_duplicado_de_link_expirado_retorna_409()
    {
        await CreateAsync(new { targetUrl = "https://exemplo.com/1", code = "PROMO26", expiresOn = "2026-10-29" });
        _factory.Clock.UtcNow = _factory.Clock.UtcNow.AddDays(10);

        var response = await _client.PostAsJsonAsync("/api/links", new { targetUrl = "https://exemplo.com/2", code = "PROMO26" });

        await AssertConflictAsync(response);
    }

    [Fact]
    public async Task Codigo_que_difere_so_em_maiusculas_e_minusculas_retorna_409()
    {
        await CreateAsync(new { targetUrl = "https://exemplo.com/1", code = "PROMO26" });

        var response = await _client.PostAsJsonAsync("/api/links", new { targetUrl = "https://exemplo.com/2", code = "promo26" });

        await AssertConflictAsync(response);
    }

    private async Task CreateAsync(object request)
    {
        var response = await _client.PostAsJsonAsync("/api/links", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task AssertConflictAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(409, problem!.Status);
        Assert.Equal(CodeInUseDetail, problem.Detail);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _database.Dispose();
    }
}
