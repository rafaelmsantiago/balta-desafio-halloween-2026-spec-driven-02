using LinkShortener.Api.Application;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LinkShortener.Api.Data;

public sealed class LinkRepository(LinksDbContext context) : ILinkRepository
{
    private const int SqliteConstraintUnique = 2067;

    public async Task<bool> TryAddAsync(Link link, CancellationToken cancellationToken)
    {
        context.Links.Add(link);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is SqliteException { SqliteExtendedErrorCode: SqliteConstraintUnique })
        {
            context.Entry(link).State = EntityState.Detached;
            return false;
        }
    }

    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken) =>
        context.Links.AnyAsync(l => l.Code == code, cancellationToken);

    public Task<Link?> FindByCodeAsync(string code, CancellationToken cancellationToken) =>
        context.Links.AsNoTracking().SingleOrDefaultAsync(l => l.Code == code, cancellationToken);
}
