using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LinkShortener.Tests.Integration;

public class PersistenceTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();

    private static readonly WebApplicationFactoryClientOptions NoRedirect = new() { AllowAutoRedirect = false };

    [Fact]
    public async Task Link_continua_funcionando_apos_reiniciar_a_aplicacao()
    {
        using (var firstRun = new LinkShortenerFactory(_database.ConnectionString))
        using (var client = firstRun.CreateClient(NoRedirect))
        {
            var created = await client.PostAsJsonAsync("/api/links", new { targetUrl = "https://exemplo.com/persistido", code = "PERSISTE" });
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            Assert.Equal(HttpStatusCode.Found, (await client.GetAsync("/PERSISTE")).StatusCode);
        }

        using var secondRun = new LinkShortenerFactory(_database.ConnectionString);
        using var restartedClient = secondRun.CreateClient(NoRedirect);

        var response = await restartedClient.GetAsync("/PERSISTE");

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("https://exemplo.com/persistido", response.Headers.Location!.ToString());
    }

    public void Dispose() => _database.Dispose();
}
