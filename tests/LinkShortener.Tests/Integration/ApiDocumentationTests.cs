using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Hosting;

namespace LinkShortener.Tests.Integration;

public class ApiDocumentationTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();

    [Fact]
    public async Task Em_desenvolvimento_o_documento_OpenAPI_lista_os_endpoints()
    {
        using var factory = new LinkShortenerFactory(_database.ConnectionString);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = document.RootElement.GetProperty("paths");
        Assert.True(paths.TryGetProperty("/api/links", out var links));
        Assert.True(paths.TryGetProperty("/api/links/{code}/stats", out _));
        Assert.True(paths.TryGetProperty("/{code}", out _));

        var schema = links.GetProperty("post").GetProperty("requestBody")
            .GetProperty("content").GetProperty("application/json").GetProperty("schema");
        Assert.True(schema.GetProperty("properties").TryGetProperty("code", out _));
        Assert.True(schema.GetProperty("properties").TryGetProperty("expiresOn", out _));
        Assert.Equal(["targetUrl"], schema.GetProperty("required").EnumerateArray().Select(e => e.GetString()));
    }

    [Fact]
    public async Task Em_desenvolvimento_a_interface_Scalar_responde()
    {
        using var factory = new LinkShortenerFactory(_database.ConnectionString);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/scalar/v1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType!.MediaType);
    }

    [Theory]
    [InlineData("/openapi/v1.json")]
    [InlineData("/scalar/v1")]
    public async Task Fora_de_desenvolvimento_a_documentacao_nao_e_exposta(string path)
    {
        using var factory = new LinkShortenerFactory(_database.ConnectionString) { Environment = Environments.Production };
        using var client = factory.CreateClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    public void Dispose() => _database.Dispose();
}
