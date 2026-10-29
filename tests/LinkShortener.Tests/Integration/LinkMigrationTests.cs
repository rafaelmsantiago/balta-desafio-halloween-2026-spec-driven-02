using LinkShortener.Api.Application;
using LinkShortener.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace LinkShortener.Tests.Integration;

public class LinkMigrationTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();

    [Fact]
    public void Migracao_cria_indice_unico_em_Code()
    {
        using (var context = CreateContext())
        {
            context.Database.Migrate();
            context.Links.Add(NewLink("ABC1234"));
            context.SaveChanges();
        }

        using (var context = CreateContext())
        {
            context.Links.Add(NewLink("ABC1234"));

            Assert.Throws<DbUpdateException>(() => context.SaveChanges());
        }
    }

    [Fact]
    public void Migracao_aceita_links_com_mesma_URL_de_destino()
    {
        using var context = CreateContext();
        context.Database.Migrate();
        context.Links.AddRange(NewLink("ABC1234"), NewLink("XYZ9876"));

        context.SaveChanges();

        Assert.Equal(2, context.Links.Count());
    }

    private LinksDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<LinksDbContext>().UseSqlite(_database.ConnectionString).Options);

    private static Link NewLink(string code) => new()
    {
        Code = code,
        TargetUrl = "https://exemplo.com/",
        CreatedAtUtc = DateTime.UtcNow,
    };

    public void Dispose() => _database.Dispose();
}
