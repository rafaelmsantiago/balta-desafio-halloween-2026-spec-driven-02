using System.Net;

namespace LinkShortener.Api.Application;

public interface IHostResolver
{
    // Lista vazia quando o host não resolve.
    Task<IReadOnlyList<IPAddress>> ResolveAsync(string host, CancellationToken cancellationToken);
}
