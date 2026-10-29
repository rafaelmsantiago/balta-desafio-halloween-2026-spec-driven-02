using LinkShortener.Api.Application;
using LinkShortener.Tests.Fakes;

namespace LinkShortener.Tests.Unit;

public class LinkServiceExpirationOnCreateTests
{
    // Fim do dia em UTC, para pegar quem confundir data local com UTC.
    private static readonly DateTimeOffset Now = new(2026, 10, 29, 23, 59, 59, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 10, 29);

    private readonly InMemoryLinkRepository _repository = new();

    private Task<CreateLinkResult> CreateAsync(DateOnly? expiresOn) =>
        new LinkService(
                _repository,
                new SequenceCodeGenerator("AAAAAAA"),
                new PublicHostChecker(new FakeHostResolver("93.184.216.34")),
                new FakeTimeProvider(Now))
            .CreateAsync(new("https://exemplo.com/", expiresOn), CancellationToken.None);

    [Fact]
    public async Task Recusa_data_anterior_a_hoje_em_UTC()
    {
        var result = await CreateAsync(Today.AddDays(-1));

        Assert.Equal(CreateLinkStatus.Invalid, result.Status);
        Assert.Equal("A data de expiração não pode ser anterior à data de hoje em UTC.", result.Error);
        Assert.Empty(_repository.Links);
    }

    [Fact]
    public async Task Aceita_a_data_de_hoje()
    {
        var result = await CreateAsync(Today);

        Assert.Equal(CreateLinkStatus.Created, result.Status);
    }

    [Fact]
    public async Task ExpiresAtUtc_e_o_inicio_do_dia_seguinte_em_UTC()
    {
        var result = await CreateAsync(new DateOnly(2026, 12, 31));

        var expected = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        Assert.Equal(expected, result.Link!.ExpiresAtUtc);
        Assert.Equal(DateTimeKind.Utc, result.Link.ExpiresAtUtc!.Value.Kind);
    }

    [Fact]
    public async Task Sem_data_ExpiresAtUtc_fica_nulo()
    {
        var result = await CreateAsync(null);

        Assert.Equal(CreateLinkStatus.Created, result.Status);
        Assert.Null(result.Link!.ExpiresAtUtc);
    }

    [Fact]
    public void ToExpiresOn_devolve_a_data_informada()
    {
        var expiresAtUtc = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        Assert.Equal(new DateOnly(2026, 12, 31), LinkExpiration.ToExpiresOn(expiresAtUtc));
        Assert.Null(LinkExpiration.ToExpiresOn(null));
    }
}
