using LinkShortener.Api.Application;
using LinkShortener.Tests.Fakes;

namespace LinkShortener.Tests.Unit;

public class LinkServiceCreateTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 29, 15, 30, 0, TimeSpan.Zero);

    private readonly InMemoryLinkRepository _repository = new();
    private FakeHostResolver _resolver = new("93.184.216.34");

    private LinkService CreateService() => new(
        _repository,
        new SequenceCodeGenerator("AAAAAAA", "BBBBBBB"),
        new PublicHostChecker(_resolver),
        new FakeTimeProvider(Now));

    [Fact]
    public async Task Cria_link_com_codigo_gerado()
    {
        var result = await CreateService().CreateAsync(new("https://exemplo.com/pagina"), CancellationToken.None);

        Assert.Equal(CreateLinkStatus.Created, result.Status);
        Assert.Equal("AAAAAAA", result.Link!.Code);
        Assert.Equal("https://exemplo.com/pagina", result.Link.TargetUrl);
        Assert.Equal(Now.UtcDateTime, result.Link.CreatedAtUtc);
        Assert.Equal(0, result.Link.AccessCount);
        Assert.Single(_repository.Links);
    }

    [Fact]
    public async Task Mesma_URL_duas_vezes_gera_dois_links_independentes()
    {
        var service = CreateService();

        var first = await service.CreateAsync(new("https://exemplo.com/"), CancellationToken.None);
        var second = await service.CreateAsync(new("https://exemplo.com/"), CancellationToken.None);

        Assert.NotEqual(first.Link!.Code, second.Link!.Code);
        Assert.NotEqual(first.Link.Id, second.Link.Id);
        Assert.Equal(2, _repository.Links.Count);
    }

    [Fact]
    public async Task Recusa_URL_com_esquema_invalido_sem_gravar()
    {
        var result = await CreateService().CreateAsync(new("ftp://exemplo.com/"), CancellationToken.None);

        Assert.Equal(CreateLinkStatus.Invalid, result.Status);
        Assert.Empty(_repository.Links);
    }

    [Fact]
    public async Task Recusa_URL_que_nao_e_publicamente_acessivel_sem_gravar()
    {
        _resolver = new FakeHostResolver("10.0.0.1");

        var result = await CreateService().CreateAsync(new("https://intranet.exemplo/"), CancellationToken.None);

        Assert.Equal(CreateLinkStatus.Invalid, result.Status);
        Assert.StartsWith("A URL de destino precisa ser publicamente acessível", result.Error);
        Assert.Empty(_repository.Links);
    }
}
