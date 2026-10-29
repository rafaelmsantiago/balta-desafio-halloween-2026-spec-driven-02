namespace LinkShortener.Api.Application;

public enum ResolveLinkStatus
{
    Redirect,
    NotFound,
    Expired,
}

public sealed record ResolveLinkResult(ResolveLinkStatus Status, string? TargetUrl = null)
{
    public static ResolveLinkResult Redirect(string targetUrl) => new(ResolveLinkStatus.Redirect, targetUrl);

    public static ResolveLinkResult NotFound() => new(ResolveLinkStatus.NotFound);

    public static ResolveLinkResult Expired() => new(ResolveLinkStatus.Expired);
}
