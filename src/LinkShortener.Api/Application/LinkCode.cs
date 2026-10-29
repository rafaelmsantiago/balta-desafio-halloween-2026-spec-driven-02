namespace LinkShortener.Api.Application;

public static class LinkCode
{
    // D-10: conversão invariante, para não depender da cultura do servidor.
    public static string Normalize(string code) => code.ToUpperInvariant();
}
