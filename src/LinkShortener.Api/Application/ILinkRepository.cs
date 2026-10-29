namespace LinkShortener.Api.Application;

public interface ILinkRepository
{
    // Falso quando o código já está em uso (índice único da D-04).
    Task<bool> TryAddAsync(Link link, CancellationToken cancellationToken);
}
