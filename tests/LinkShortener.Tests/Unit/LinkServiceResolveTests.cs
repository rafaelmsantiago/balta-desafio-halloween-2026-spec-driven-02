using LinkShortener.Api.Application;
using LinkShortener.Tests.Fakes;

namespace LinkShortener.Tests.Unit;

public class LinkServiceResolveTests
{
    private readonly InMemoryLinkRepository _repository = new();
    private readonly LinkService _service;

    public LinkServiceResolveTests()
    {
        _repository.Links.Add(new Link { Id = 1, Code = "PROMO26", TargetUrl = "https://exemplo.com/destino" });
        _service = new LinkService(
            _repository,
            new SequenceCodeGenerator("AAAAAAA"),
            new PublicHostChecker(new FakeHostResolver("93.184.216.34")),
            new FakeTimeProvider(new DateTimeOffset(2026, 10, 29, 12, 0, 0, TimeSpan.Zero)));
    }

    [Theory]
    [InlineData("PROMO26")]
    [InlineData("promo26")]
    public async Task Redireciona_para_o_destino(string code)
    {
        var result = await _service.ResolveAsync(code, CancellationToken.None);

        Assert.Equal(ResolveLinkStatus.Redirect, result.Status);
        Assert.Equal("https://exemplo.com/destino", result.TargetUrl);
    }

    [Fact]
    public async Task Acessos_repetidos_incrementam_o_contador_a_cada_requisicao()
    {
        for (var i = 0; i < 3; i++)
            await _service.ResolveAsync("PROMO26", CancellationToken.None);

        Assert.Equal(3, _repository.Links.Single().AccessCount);
    }

    [Fact]
    public async Task Codigo_inexistente_nao_e_encontrado()
    {
        var result = await _service.ResolveAsync("NAOEXISTE", CancellationToken.None);

        Assert.Equal(ResolveLinkStatus.NotFound, result.Status);
    }
}
