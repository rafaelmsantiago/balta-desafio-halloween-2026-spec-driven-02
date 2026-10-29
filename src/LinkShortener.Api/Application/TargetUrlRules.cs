using System.Diagnostics.CodeAnalysis;

namespace LinkShortener.Api.Application;

public static class TargetUrlRules
{
    public const int MaxLength = 2048;

    // RN-02 (esquema) e caso de borda de 2048 caracteres.
    public static bool TryValidate(
        string targetUrl,
        [NotNullWhen(true)] out Uri? uri,
        [NotNullWhen(false)] out string? error)
    {
        uri = null;

        if (targetUrl.Length > MaxLength)
        {
            error = $"A URL de destino não pode ter mais de {MaxLength} caracteres.";
            return false;
        }

        if (!Uri.TryCreate(targetUrl, UriKind.Absolute, out var parsed)
            || (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps))
        {
            error = "A URL de destino precisa ser um endereço http ou https válido.";
            return false;
        }

        uri = parsed;
        error = null;
        return true;
    }
}
