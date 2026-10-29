using System.Net;
using LinkShortener.Api.Application;

namespace LinkShortener.Tests.Fakes;

public sealed class FakeHostResolver(params string[] addresses) : IHostResolver
{
    public string? LastHost { get; private set; }

    public Task<IReadOnlyList<IPAddress>> ResolveAsync(string host, CancellationToken cancellationToken)
    {
        LastHost = host;
        return Task.FromResult<IReadOnlyList<IPAddress>>(addresses.Select(IPAddress.Parse).ToArray());
    }
}
