using System.Globalization;
using System.Text.Json;
using LinkShortener.Api.Application;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace LinkShortener.Api.Endpoints;

public static class LinkEndpoints
{
    private const string DateFormat = "yyyy-MM-dd";

    public sealed record CreateLinkRequest(string? TargetUrl, string? Code, string? ExpiresOn);

    public sealed record CreateLinkResponse(string ShortUrl, string Code, string? ExpiresOn);

    public static void MapLinkEndpoints(this IEndpointRouteBuilder app)
    {
        // Os metadados abaixo só descrevem o contrato no documento OpenAPI (D-13).
        app.MapPost("/api/links", CreateAsync)
            .WithTags("Links")
            .WithSummary("Cria um link curto")
            .AddOpenApiOperationTransformer(DescribeCreateRequestAsync)
            .Produces<CreateLinkResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet("/api/links/{code}/stats", GetStatsAsync)
            .WithTags("Links")
            .WithSummary("Consulta o total de acessos de um link")
            .Produces<LinkStats>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        app.MapGet("/{code}", RedirectAsync)
            .WithTags("Redirecionamento")
            .WithSummary("Redireciona para a URL de destino")
            .Produces(StatusCodes.Status302Found)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status410Gone);
    }

    // Descreve o corpo sem usar Accepts, que recusaria com 415 o que não fosse JSON
    // antes de o endpoint responder 400 em português (D-09).
    private static async Task DescribeCreateRequestAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var schema = await context.GetOrCreateSchemaAsync(typeof(CreateLinkRequest), null, cancellationToken);

        // Só targetUrl é obrigatório; code e expiresOn são opcionais (RF-03, RF-06).
        schema.Required = new HashSet<string> { "targetUrl" };
        Describe(schema, "targetUrl", "URL de destino, http ou https, com até 2048 caracteres.");
        Describe(schema, "code", "Código próprio opcional, de 4 a 20 caracteres, com letras de A a Z e números de 1 a 9.");
        Describe(schema, "expiresOn", "Data de expiração opcional, no formato yyyy-MM-dd, em UTC.");

        operation.RequestBody = new OpenApiRequestBody
        {
            Required = true,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new() { Schema = schema },
            },
        };
    }

    private static void Describe(OpenApiSchema schema, string property, string description)
    {
        if (schema.Properties?.TryGetValue(property, out var propertySchema) == true
            && propertySchema is OpenApiSchema concrete)
            concrete.Description = description;
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
        HttpRequest httpRequest,
        LinkService service,
        CancellationToken cancellationToken)
    {
        // O corpo é lido aqui, e não pelo binding automático, para que payload
        // malformado também responda 400 com mensagem em português (D-09).
        var request = await ReadRequestAsync(httpRequest, cancellationToken);
        if (request is null)
            return BadRequest("O corpo da requisição precisa ser um JSON válido com targetUrl, code e expiresOn como texto.");

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

    private static async Task<CreateLinkRequest?> ReadRequestAsync(HttpRequest httpRequest, CancellationToken cancellationToken)
    {
        if (!httpRequest.HasJsonContentType())
            return null;

        try
        {
            return await httpRequest.ReadFromJsonAsync<CreateLinkRequest>(cancellationToken);
        }
        catch (JsonException)
        {
            return null;
        }
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
