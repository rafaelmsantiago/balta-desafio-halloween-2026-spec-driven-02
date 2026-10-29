using System.Globalization;
using LinkShortener.Api.Application;

namespace LinkShortener.Api.Endpoints;

public static class LinkEndpoints
{
    private const string DateFormat = "yyyy-MM-dd";

    public sealed record CreateLinkRequest(string? TargetUrl, string? Code, string? ExpiresOn);

    public sealed record CreateLinkResponse(string ShortUrl, string Code, string? ExpiresOn);

    public static void MapLinkEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/links", CreateAsync);
        app.MapGet("/api/links/{code}/stats", GetStatsAsync);
        app.MapGet("/{code}", RedirectAsync);
    }

    private static async Task<IResult> GetStatsAsync(string code, LinkService service, CancellationToken cancellationToken)
    {
        var stats = await service.GetStatsAsync(code, cancellationToken);

        return stats is null ? NotFound() : Results.Ok(stats);
    }

    private static async Task<IResult> RedirectAsync(string code, LinkService service, CancellationToken cancellationToken)
    {
        var result = await service.ResolveAsync(code, cancellationToken);

        return result.Status switch
        {
            ResolveLinkStatus.Redirect => Results.Redirect(result.TargetUrl!),
            ResolveLinkStatus.Expired => Results.Problem(
                statusCode: StatusCodes.Status410Gone,
                title: "Link expirado",
                detail: "O link informado expirou."),
            _ => NotFound(),
        };
    }

    private static async Task<IResult> CreateAsync(
        CreateLinkRequest request,
        HttpRequest httpRequest,
        LinkService service,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TargetUrl))
            return BadRequest("O campo targetUrl é obrigatório.");

        DateOnly? expiresOn = null;
        if (request.ExpiresOn is not null)
        {
            if (!DateOnly.TryParseExact(request.ExpiresOn, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                return BadRequest($"O campo expiresOn precisa ser uma data no formato {DateFormat}.");

            expiresOn = date;
        }

        var result = await service.CreateAsync(
            new CreateLinkCommand(request.TargetUrl, expiresOn, request.Code), cancellationToken);

        return result.Status switch
        {
            CreateLinkStatus.Created => Created(httpRequest, result.Link!),
            CreateLinkStatus.Invalid => BadRequest(result.Error!),
            CreateLinkStatus.CodeInUse => Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Código em uso",
                detail: result.Error),
            _ => Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Serviço indisponível",
                detail: result.Error),
        };
    }

    private static IResult Created(HttpRequest httpRequest, Link link)
    {
        var shortUrl = $"{httpRequest.Scheme}://{httpRequest.Host}{httpRequest.PathBase}/{link.Code}";
        var expiresOn = LinkExpiration.ToExpiresOn(link.ExpiresAtUtc)?.ToString(DateFormat, CultureInfo.InvariantCulture);

        return Results.Created(shortUrl, new CreateLinkResponse(shortUrl, link.Code, expiresOn));
    }

    private static IResult NotFound() =>
        Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Link não encontrado", detail: "O link informado não existe.");

    private static IResult BadRequest(string detail) =>
        Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Requisição inválida", detail: detail);
}
