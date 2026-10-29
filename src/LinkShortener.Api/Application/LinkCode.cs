namespace LinkShortener.Api.Application;

public static class LinkCode
{
    // RF-02 e RN-05: A-Z maiúsculo e dígitos de 1 a 9, sem o 0.
    public const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ123456789";

    public const int GeneratedLength = 7;

    // D-10: conversão invariante, para não depender da cultura do servidor.
    public static string Normalize(string code) => code.ToUpperInvariant();
}
