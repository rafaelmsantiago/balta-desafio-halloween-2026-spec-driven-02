using System.Diagnostics.CodeAnalysis;

namespace LinkShortener.Api.Application;

public static class LinkCode
{
    // RF-02 e RN-05: A-Z maiúsculo e dígitos de 1 a 9, sem o 0.
    public const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ123456789";

    public const int GeneratedLength = 7;

    public const int CustomMinLength = 4;

    public const int CustomMaxLength = 20;

    // D-10: conversão invariante, para não depender da cultura do servidor.
    public static string Normalize(string code) => code.ToUpperInvariant();

    // RF-03: normaliza primeiro e só depois valida contra o alfabeto (D-10).
    public static bool TryNormalizeCustom(
        string code,
        [NotNullWhen(true)] out string? normalized,
        [NotNullWhen(false)] out string? error)
    {
        normalized = null;
        var upper = Normalize(code);

        if (upper.Length < CustomMinLength || upper.Length > CustomMaxLength)
        {
            error = $"O código precisa ter de {CustomMinLength} a {CustomMaxLength} caracteres.";
            return false;
        }

        if (!upper.All(Alphabet.Contains))
        {
            error = "O código só pode ter letras de A a Z e números de 1 a 9, sem espaços nem caracteres especiais.";
            return false;
        }

        normalized = upper;
        error = null;
        return true;
    }
}
