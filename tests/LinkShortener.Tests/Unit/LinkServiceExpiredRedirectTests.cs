using LinkShortener.Api.Application;
using LinkShortener.Tests.Fakes;

namespace LinkShortener.Tests.Unit;

public class LinkServiceExpiredRedirectTests
{
    private readonly InMemoryLinkRepository _repository = new();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 10, 29, 12, 0, 0, TimeSpan.Zero));
    private readonly LinkService _service;

    public LinkServiceExpiredRedirectTests()
    {
        _service = new LinkService(
            _repository,
            new SequenceCodeGenerator("AAAAAAA"),
            new PublicHostChecker(new FakeHostResolver("93.184.216.34")),
            _clock);
    }

    [Fact]
    public async Task Link_vale_ate_23_59_59_UTC_da_data_e_expira_no_instante_seguinte()
    {
        var created = await _service.CreateAsync(
            new("https://exemplo.com/", ExpiresOn: new DateOnly(2026, 10, 30)), CancellationToken.None);
        var code = created.Link!.Code;

        _clock.UtcNow = new DateTimeOffset(2026, 10, 30, 23, 59, 59, 999, TimeSpan.Zero);
        Assert.Equal(ResolveLinkStatus.Redirect, (await _service.ResolveAsync(code, CancellationToken.None)).Status);

        _clock.UtcNow = new DateTimeOffset(2026, 10, 31, 0, 0, 0, TimeSpan.Zero);
        Assert.Equal(ResolveLinkStatus.Expired, (await _service.ResolveAsync(code, CancellationToken.None)).Status);
    }

    [Fact]
    public async Task Link_expirado_nao_incrementa_o_contador()
    {
        var created = await _service.CreateAsync(
            new("https://exemplo.com/", ExpiresOn: new DateOnly(2026, 10, 29)), CancellationToken.None);
        await _service.ResolveAsync(created.Link!.Code, CancellationToken.None);

        _clock.UtcNow = _clock.UtcNow.AddDays(1);
        await _service.ResolveAsync(created.Link.Code, CancellationToken.None);

        Assert.Equal(1, _repository.Links.Single().AccessCount);
    }

    [Fact]
    public async Task Link_sem_data_nunca_expira()
    {
        var created = await _service.CreateAsync(new("https://exemplo.com/"), CancellationToken.None);

        _clock.UtcNow = _clock.UtcNow.AddYears(100);
        var result = await _service.ResolveAsync(created.Link!.Code, CancellationToken.None);

        Assert.Equal(ResolveLinkStatus.Redirect, result.Status);
    }
}
