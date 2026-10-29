using LinkShortener.Api.Application;
using LinkShortener.Tests.Fakes;

namespace LinkShortener.Tests.Unit;

public class LinkServiceCustomCodeTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);

    private readonly InMemoryLinkRepository _repository = new();
    private readonly SequenceCodeGenerator _generator = new("AUTO123");

    private Task<CreateLinkResult> CreateAsync(string code) =>
        new LinkService(
                _repository,
                _generator,
                new PublicHostChecker(new FakeHostResolver("93.184.216.34")),
                new FakeTimeProvider(Now))
            .CreateAsync(new("https://exemplo.com/", Code: code), CancellationToken.None);

    [Fact]
    public async Task Usa_o_codigo_proprio_sem_gerar_codigo()
    {
        var result = await CreateAsync("PROMO26");

        Assert.Equal(CreateLinkStatus.Created, result.Status);
        Assert.Equal("PROMO26", result.Link!.Code);
        Assert.Equal(0, _generator.Calls);
    }

    [Fact]
    public async Task Codigo_em_minusculas_e_gravado_em_maiusculas()
    {
        var result = await CreateAsync("promo26");

        Assert.Equal(CreateLinkStatus.Created, result.Status);
        Assert.Equal("PROMO26", Assert.Single(_repository.Links).Code);
    }

    [Fact]
    public async Task Recusa_codigo_em_uso()
    {
        _repository.Links.Add(new Link { Id = 1, Code = "PROMO26", TargetUrl = "https://outro.exemplo/" });

        var result = await CreateAsync("PROMO26");

        Assert.Equal(CreateLinkStatus.CodeInUse, result.Status);
        Assert.Equal(
            "O código informado já está em uso por outro link, inclusive se ele já expirou. Escolha outro código.",
            result.Error);
        Assert.Single(_repository.Links);
    }

    [Fact]
    public async Task Recusa_codigo_em_uso_por_link_expirado()
    {
        _repository.Links.Add(new Link
        {
            Id = 1,
            Code = "PROMO26",
            TargetUrl = "https://outro.exemplo/",
            ExpiresAtUtc = Now.UtcDateTime.AddDays(-30),
        });

        var result = await CreateAsync("PROMO26");

        Assert.Equal(CreateLinkStatus.CodeInUse, result.Status);
    }

    [Fact]
    public async Task Recusa_codigo_em_uso_que_difere_so_em_maiusculas_e_minusculas()
    {
        _repository.Links.Add(new Link { Id = 1, Code = "PROMO26", TargetUrl = "https://outro.exemplo/" });

        var result = await CreateAsync("Promo26");

        Assert.Equal(CreateLinkStatus.CodeInUse, result.Status);
    }

    [Fact]
    public async Task Recusa_codigo_com_formato_invalido()
    {
        var result = await CreateAsync("PROMO-0");

        Assert.Equal(CreateLinkStatus.Invalid, result.Status);
        Assert.Empty(_repository.Links);
    }
}
