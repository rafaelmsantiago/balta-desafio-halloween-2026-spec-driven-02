namespace LinkShortener.Api.Application;

public interface ILinkRepository
{
    // Falso quando o código já está em uso (índice único da D-04).
    Task<bool> TryAddAsync(Link link, CancellationToken cancellationToken);

    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken);

    Task<Link?> FindByCodeAsync(string code, CancellationToken cancellationToken);
}
