using LinkShortener.Api.Application;

namespace LinkShortener.Api.Endpoints;

public static class LinkEndpoints
{
    public sealed record CreateLinkRequest(string? TargetUrl);

    public sealed record CreateLinkResponse(string ShortUrl, string Code, string? ExpiresOn);

    public static void MapLinkEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/links", CreateAsync);
    }

    private static async Task<IResult> CreateAsync(
        CreateLinkRequest request,
        HttpRequest httpRequest,
        LinkService service,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TargetUrl))
            return BadRequest("O campo targetUrl é obrigatório.");

        var result = await service.CreateAsync(new CreateLinkCommand(request.TargetUrl), cancellationToken);

        return result.Status switch
        {
            CreateLinkStatus.Created => Created(httpRequest, result.Link!),
            CreateLinkStatus.Invalid => BadRequest(result.Error!),
            _ => Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Serviço indisponível",
                detail: result.Error),
        };
    }

    private static IResult Created(HttpRequest httpRequest, Link link)
    {
        var shortUrl = $"{httpRequest.Scheme}://{httpRequest.Host}{httpRequest.PathBase}/{link.Code}";

        return Results.Created(shortUrl, new CreateLinkResponse(shortUrl, link.Code, null));
    }

    private static IResult BadRequest(string detail) =>
        Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Requisição inválida", detail: detail);
}
