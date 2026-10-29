using System.Net;
using System.Net.Http.Json;
using LinkShortener.Api.Data;
using Microsoft.Extensions.DependencyInjection;

namespace LinkShortener.Tests.Integration;

public class CreateLinkTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();
    private readonly LinkShortenerFactory _factory;

    public CreateLinkTests() => _factory = new LinkShortenerFactory(_database.ConnectionString);

    private sealed record CreatedLink(string ShortUrl, string Code, string? ExpiresOn);

    [Fact]
    public async Task Cria_link_e_retorna_201()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/links", new { targetUrl = "https://exemplo.com/pagina" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreatedLink>();
        Assert.Matches("^[A-Z1-9]{7}$", body!.Code);
        Assert.Equal($"http://localhost/{body.Code}", body.ShortUrl);
        Assert.Null(body.ExpiresOn);
        Assert.Equal(body.ShortUrl, response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Mesma_URL_duas_vezes_gera_dois_codigos_com_contadores_independentes()
    {
        var client = _factory.CreateClient();

        var first = await CreateAsync(client, "https://exemplo.com/mesma");
        var second = await CreateAsync(client, "https://exemplo.com/mesma");

        Assert.NotEqual(first.Code, second.Code);

        using var scope = _factory.Services.CreateScope();
        var links = scope.ServiceProvider.GetRequiredService<LinksDbContext>().Links
            .Where(l => l.TargetUrl == "https://exemplo.com/mesma")
            .ToList();
        Assert.Equal(2, links.Count);
        Assert.All(links, l => Assert.Equal(0, l.AccessCount));
    }

    [Fact]
    public async Task Cria_link_com_data_de_expiracao()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/links", new { targetUrl = "https://exemplo.com/", expiresOn = "2026-12-31" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreatedLink>();
        Assert.Equal("2026-12-31", body!.ExpiresOn);

        using var scope = _factory.Services.CreateScope();
        var link = scope.ServiceProvider.GetRequiredService<LinksDbContext>().Links.Single(l => l.Code == body.Code);
        Assert.Equal(new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc), link.ExpiresAtUtc);
        Assert.Equal(DateTimeKind.Utc, link.ExpiresAtUtc!.Value.Kind);
    }

    private static async Task<CreatedLink> CreateAsync(HttpClient client, string targetUrl)
    {
        var response = await client.PostAsJsonAsync("/api/links", new { targetUrl });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreatedLink>())!;
    }

    public void Dispose()
    {
        _factory.Dispose();
        _database.Dispose();
    }
}
