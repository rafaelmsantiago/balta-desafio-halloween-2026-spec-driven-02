namespace LinkShortener.Api.Application;

public enum CreateLinkStatus
{
    Created,
    Invalid,
    Unavailable,
}

public sealed record CreateLinkResult(CreateLinkStatus Status, Link? Link = null, string? Error = null)
{
    public static CreateLinkResult Created(Link link) => new(CreateLinkStatus.Created, link);

    public static CreateLinkResult Invalid(string error) => new(CreateLinkStatus.Invalid, Error: error);

    public static CreateLinkResult Unavailable(string error) => new(CreateLinkStatus.Unavailable, Error: error);
}
