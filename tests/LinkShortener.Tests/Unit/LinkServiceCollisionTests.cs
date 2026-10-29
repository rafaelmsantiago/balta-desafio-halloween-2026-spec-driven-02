using LinkShortener.Api.Application;
using LinkShortener.Tests.Fakes;

namespace LinkShortener.Tests.Unit;

public class LinkServiceCollisionTests
{
    private readonly InMemoryLinkRepository _repository = new();

    public LinkServiceCollisionTests()
    {
        _repository.Links.Add(new Link { Id = 1, Code = "EMUSO11", TargetUrl = "https://exemplo.com/" });
    }

    private LinkService CreateService(ICodeGenerator generator) => new(
        _repository,
        generator,
        new PublicHostChecker(new FakeHostResolver("93.184.216.34")),
        new FakeTimeProvider(DateTimeOffset.UtcNow));

    [Fact]
    public async Task Colisao_gera_outro_codigo()
    {
        var generator = new SequenceCodeGenerator("EMUSO11", "LIVRE22");

        var result = await CreateService(generator).CreateAsync(new("https://exemplo.com/"), CancellationToken.None);

        Assert.Equal(CreateLinkStatus.Created, result.Status);
        Assert.Equal("LIVRE22", result.Link!.Code);
        Assert.Equal(2, generator.Calls);
    }

    [Fact]
    public async Task Quarta_colisao_ainda_tenta_a_quinta_vez()
    {
        var generator = new SequenceCodeGenerator("EMUSO11", "EMUSO11", "EMUSO11", "EMUSO11", "LIVRE22");

        var result = await CreateService(generator).CreateAsync(new("https://exemplo.com/"), CancellationToken.None);

        Assert.Equal(CreateLinkStatus.Created, result.Status);
        Assert.Equal(5, generator.Calls);
    }

    [Fact]
    public async Task Apos_5_colisoes_retorna_erro()
    {
        var generator = new SequenceCodeGenerator("EMUSO11");

        var result = await CreateService(generator).CreateAsync(new("https://exemplo.com/"), CancellationToken.None);

        Assert.Equal(CreateLinkStatus.Unavailable, result.Status);
        Assert.Equal("Não foi possível gerar um código livre para o link agora. Tente novamente em instantes.", result.Error);
        Assert.Equal(5, generator.Calls);
        Assert.Single(_repository.Links);
    }
}
