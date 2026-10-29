using LinkShortener.Api.Application;

namespace LinkShortener.Tests.Fakes;

public sealed class InMemoryLinkRepository : ILinkRepository
{
    public List<Link> Links { get; } = [];

    public Task<bool> TryAddAsync(Link link, CancellationToken cancellationToken)
    {
        // Simula o índice único da D-04.
        if (Links.Any(l => l.Code == link.Code))
            return Task.FromResult(false);

        link.Id = Links.Count + 1;
        Links.Add(link);
        return Task.FromResult(true);
    }

    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken) =>
        Task.FromResult(Links.Any(l => l.Code == code));
}
