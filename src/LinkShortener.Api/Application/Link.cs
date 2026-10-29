namespace LinkShortener.Api.Application;

public sealed class Link
{
    public int Id { get; set; }

    // Sempre em maiúsculas (D-10).
    public required string Code { get; set; }

    public required string TargetUrl { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    // Limite exclusivo: o link vale enquanto o instante atual for menor (D-06).
    public DateTime? ExpiresAtUtc { get; set; }

    public long AccessCount { get; set; }
}
