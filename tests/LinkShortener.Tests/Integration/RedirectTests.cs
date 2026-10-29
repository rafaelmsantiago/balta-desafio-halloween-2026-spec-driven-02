using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LinkShortener.Tests.Integration;

public class RedirectTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();
    private readonly LinkShortenerFactory _factory;
    private readonly HttpClient _client;

    public RedirectTests()
    {
        _factory = new LinkShortenerFactory(_database.ConnectionString);
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    [Theory]
    [InlineData("PROMO26")]
    [InlineData("promo26")]
    public async Task Redireciona_com_302(string code)
    {
        var created = await _client.PostAsJsonAsync("/api/links", new { targetUrl = "https://exemplo.com/destino", code = "PROMO26" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var response = await _client.GetAsync($"/{code}");

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("https://exemplo.com/destino", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Codigo_inexistente_retorna_404_com_ProblemDetails()
    {
        var response = await _client.GetAsync("/NAOEXISTE");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("O link informado não existe.", problem!.Detail);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _database.Dispose();
    }
}
