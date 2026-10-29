using LinkShortener.Api.Application;
using LinkShortener.Tests.Fakes;

namespace LinkShortener.Tests.Unit;

public class LinkServiceStatsTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);

    private readonly InMemoryLinkRepository _repository = new();
    private readonly LinkService _service;

    public LinkServiceStatsTests()
    {
        _service = new LinkService(
            _repository,
            new SequenceCodeGenerator("AAAAAAA"),
            new PublicHostChecker(new FakeHostResolver("93.184.216.34")),
            new FakeTimeProvider(Now));
    }

    [Theory]
    [InlineData("PROMO26")]
    [InlineData("promo26")]
    public async Task Devolve_o_total_de_acessos(string code)
    {
        _repository.Links.Add(new Link { Id = 1, Code = "PROMO26", TargetUrl = "https://exemplo.com/", AccessCount = 42 });

        var stats = await _service.GetStatsAsync(code, CancellationToken.None);

        Assert.Equal(new LinkStats("PROMO26", 42), stats);
    }

    [Fact]
    public async Task Devolve_o_total_de_link_expirado()
    {
        _repository.Links.Add(new Link
        {
            Id = 1,
            Code = "PROMO26",
            TargetUrl = "https://exemplo.com/",
            AccessCount = 7,
            ExpiresAtUtc = Now.UtcDateTime.AddDays(-1),
        });

        var stats = await _service.GetStatsAsync("PROMO26", CancellationToken.None);

        Assert.Equal(7, stats!.AccessCount);
    }

    [Fact]
    public async Task Codigo_inexistente_devolve_nulo()
    {
        Assert.Null(await _service.GetStatsAsync("NAOEXISTE", CancellationToken.None));
    }
}
