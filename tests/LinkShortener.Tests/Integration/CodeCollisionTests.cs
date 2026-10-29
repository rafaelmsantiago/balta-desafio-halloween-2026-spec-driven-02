using System.Net;
using System.Net.Http.Json;
using LinkShortener.Tests.Fakes;
using Microsoft.AspNetCore.Mvc;

namespace LinkShortener.Tests.Integration;

public class CodeCollisionTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();
    private readonly LinkShortenerFactory _factory;

    public CodeCollisionTests() => _factory = new LinkShortenerFactory(_database.ConnectionString)
    {
        CodeGenerator = new SequenceCodeGenerator("FIXO123"),
    };

    [Fact]
    public async Task Limite_de_tentativas_atingido_retorna_503_com_ProblemDetails()
    {
        var client = _factory.CreateClient();
        var first = await client.PostAsJsonAsync("/api/links", new { targetUrl = "https://exemplo.com/1" });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var response = await client.PostAsJsonAsync("/api/links", new { targetUrl = "https://exemplo.com/2" });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(503, problem!.Status);
        Assert.Equal("Não foi possível gerar um código livre para o link agora. Tente novamente em instantes.", problem.Detail);
    }

    public void Dispose()
    {
        _factory.Dispose();
        _database.Dispose();
    }
}
