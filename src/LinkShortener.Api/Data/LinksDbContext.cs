using LinkShortener.Api.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LinkShortener.Api.Data;

public sealed class LinksDbContext(DbContextOptions<LinksDbContext> options) : DbContext(options)
{
    public DbSet<Link> Links => Set<Link>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // O SQLite não guarda o Kind; tudo é gravado em UTC e lido de volta como UTC.
        var utc = new ValueConverter<DateTime, DateTime>(
            value => value,
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

        modelBuilder.Entity<Link>(link =>
        {
            link.Property(l => l.Code).HasMaxLength(20).IsRequired();
            link.Property(l => l.TargetUrl).HasMaxLength(TargetUrlRules.MaxLength).IsRequired();
            link.Property(l => l.CreatedAtUtc).HasConversion(utc);
            link.Property(l => l.ExpiresAtUtc).HasConversion(utc);

            // D-04: unicidade garantida pelo banco, inclusive contra código de link expirado.
            link.HasIndex(l => l.Code).IsUnique();
        });
    }
}
