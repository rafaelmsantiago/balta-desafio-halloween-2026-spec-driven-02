namespace LinkShortener.Api.Application;

public sealed record CreateLinkCommand(string TargetUrl, DateOnly? ExpiresOn = null);
