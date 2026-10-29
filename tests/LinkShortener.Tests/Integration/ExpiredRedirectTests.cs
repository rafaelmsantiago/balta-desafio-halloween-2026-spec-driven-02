using System.Net;
using System.Net.Http.Json;
using LinkShortener.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace LinkShortener.Tests.Integration;

public class ExpiredRedirectTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();
    private readonly LinkShortenerFactory _factory;
    private readonly HttpClient _client;

    public ExpiredRedirectTests()
    {
        _factory = new LinkShortenerFactory(_database.ConnectionString);
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task Link_vale_ate_23_59_59_UTC_e_depois_retorna_410_sem_contar_acesso()
    {
        await CreateAsync(new { targetUrl = "https://exemplo.com/", code = "PROMO26", expiresOn = "2026-10-30" });

        _factory.Clock.UtcNow = new DateTimeOffset(2026, 10, 30, 23, 59, 59, TimeSpan.Zero);
        Assert.Equal(HttpStatusCode.Found, (await _client.GetAsync("/PROMO26")).StatusCode);

        _factory.Clock.UtcNow = new DateTimeOffset(2026, 10, 31, 0, 0, 0, TimeSpan.Zero);
        var response = await _client.GetAsync("/PROMO26");

        Assert.Equal(HttpStatusCode.Gone, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("O link informado expirou.", problem!.Detail);
        Assert.Equal(1, AccessCountOf("PROMO26"));
    }

    [Fact]
    public async Task Link_sem_data_continua_redirecionando()
    {
        await CreateAsync(new { targetUrl = "https://exemplo.com/", code = "SEMDATA" });

        _factory.Clock.UtcNow = _factory.Clock.UtcNow.AddYears(10);

        Assert.Equal(HttpStatusCode.Found, (await _client.GetAsync("/SEMDATA")).StatusCode);
    }

    private async Task CreateAsync(object request)
    {
        var response = await _client.PostAsJsonAsync("/api/links", request);
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
