using System.Diagnostics.CodeAnalysis;

namespace LinkShortener.Api.Application;

public static class LinkExpiration
{
    // D-06: a data vira o início do dia seguinte em UTC, limite exclusivo.
    // RN-07: a menor data aceita é a de hoje em UTC.
    public static bool TryGetExpiresAtUtc(
        DateOnly? expiresOn,
        DateTimeOffset utcNow,
        out DateTime? expiresAtUtc,
        [NotNullWhen(false)] out string? error)
    {
        expiresAtUtc = null;
        error = null;

        if (expiresOn is not { } date)
            return true;

        var today = DateOnly.FromDateTime(utcNow.UtcDateTime);
        if (date < today)
        {
            error = "A data de expiração não pode ser anterior à data de hoje em UTC.";
            return false;
        }

        expiresAtUtc = date.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        return true;
    }

    // Data informada na criação, recuperada a partir do limite exclusivo.
    public static DateOnly? ToExpiresOn(DateTime? expiresAtUtc) =>
        expiresAtUtc is { } value ? DateOnly.FromDateTime(value).AddDays(-1) : null;
}
