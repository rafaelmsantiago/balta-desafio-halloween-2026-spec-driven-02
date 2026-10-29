using System.Net;
using System.Net.Http.Json;
using System.Text;
using LinkShortener.Tests.Fakes;
using Microsoft.AspNetCore.Mvc;

namespace LinkShortener.Tests.Integration;

public class ValidationErrorTests : IDisposable
{
    private const string MalformedPayload =
        "O corpo da requisição precisa ser um JSON válido com targetUrl, code e expiresOn como texto.";

    private readonly SqliteTestDatabase _database = new();
    private readonly LinkShortenerFactory _factory;

    public ValidationErrorTests() => _factory = new LinkShortenerFactory(_database.ConnectionString);

    public static TheoryData<string, string> InvalidRequests => new()
    {
        { """{ }""", "O campo targetUrl é obrigatório." },
        { """{ "targetUrl": "ftp://exemplo.com/" }""", "A URL de destino precisa ser um endereço http ou https válido." },
        { $$"""{ "targetUrl": "https://exemplo.com/{{new string('a', 2030)}}" }""", "A URL de destino não pode ter mais de 2048 caracteres." },
        { """{ "targetUrl": "https://exemplo.com/", "expiresOn": "2026-10-28" }""", "A data de expiração não pode ser anterior à data de hoje em UTC." },
        { """{ "targetUrl": "https://exemplo.com/", "expiresOn": "29/10/2026" }""", "O campo expiresOn precisa ser uma data no formato yyyy-MM-dd." },
        { """{ "targetUrl": "https://exemplo.com/", "code": "AB1" }""", "O código precisa ter de 4 a 20 caracteres." },
        { """{ "targetUrl": "https://exemplo.com/", "code": "MEU LINK0" }""", "O código só pode ter letras de A a Z e números de 1 a 9, sem espaços nem caracteres especiais." },
        { """{ "targetUrl": "https://exemplo.com/" """, MalformedPayload },
        { """{ "targetUrl": 123 }""", MalformedPayload },
        { """null""", MalformedPayload },
    };

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public async Task Entrada_invalida_retorna_400_com_ProblemDetails_em_portugues(string json, string expectedDetail)
    {
        var response = await PostAsync(json);

        await AssertBadRequestAsync(response, expectedDetail);
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("192.168.0.10")]
    [InlineData("169.254.169.254")]
    public async Task URL_que_nao_e_publicamente_acessivel_retorna_400(string address)
    {
        _factory.HostResolver = new FakeHostResolver(address);

        var response = await PostAsync("""{ "targetUrl": "https://interno.exemplo/" }""");

        await AssertBadRequestAsync(
            response,
            "A URL de destino precisa ser publicamente acessível: o host deve existir e não pode apontar para a rede interna, para a própria máquina nem para endereços reservados.");
    }

    [Fact]
    public async Task Host_que_nao_resolve_retorna_400()
    {
        _factory.HostResolver = new FakeHostResolver();

        var response = await PostAsync("""{ "targetUrl": "https://nao-existe.exemplo/" }""");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Corpo_que_nao_e_JSON_retorna_400()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/links", new StringContent("targetUrl=https://exemplo.com/", Encoding.UTF8, "text/plain"));

        await AssertBadRequestAsync(response, MalformedPayload);
    }

    private async Task<HttpResponseMessage> PostAsync(string json)
    {
        using var client = _factory.CreateClient();
        return await client.PostAsync("/api/links", new StringContent(json, Encoding.UTF8, "application/json"));
    }

    private static async Task AssertBadRequestAsync(HttpResponseMessage response, string expectedDetail)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(400, problem!.Status);
        Assert.Equal("Requisição inválida", problem.Title);
        Assert.Equal(expectedDetail, problem.Detail);
    }

    public void Dispose()
    {
        _factory.Dispose();
        _database.Dispose();
    }
}
