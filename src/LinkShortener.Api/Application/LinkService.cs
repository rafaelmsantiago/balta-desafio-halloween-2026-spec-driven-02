namespace LinkShortener.Api.Application;

public sealed class LinkService(
    ILinkRepository links,
    ICodeGenerator codeGenerator,
    PublicHostChecker hostChecker,
    TimeProvider timeProvider)
{
    public const int MaxGenerationAttempts = 5;

    private const string CodeInUseError =
        "O código informado já está em uso por outro link, inclusive se ele já expirou. Escolha outro código.";

    public async Task<CreateLinkResult> CreateAsync(CreateLinkCommand command, CancellationToken cancellationToken)
    {
        if (!TargetUrlRules.TryValidate(command.TargetUrl, out var targetUri, out var urlError))
            return CreateLinkResult.Invalid(urlError);

        var utcNow = timeProvider.GetUtcNow();
        if (!LinkExpiration.TryGetExpiresAtUtc(command.ExpiresOn, utcNow, out var expiresAtUtc, out var expirationError))
            return CreateLinkResult.Invalid(expirationError);

        string? customCode = null;
        if (command.Code is not null && !LinkCode.TryNormalizeCustom(command.Code, out customCode, out var codeError))
            return CreateLinkResult.Invalid(codeError);

        if (!await hostChecker.IsPubliclyAccessibleAsync(targetUri, cancellationToken))
            return CreateLinkResult.Invalid(
                "A URL de destino precisa ser publicamente acessível: o host deve existir e não pode apontar para a rede interna, para a própria máquina nem para endereços reservados.");

        Link NewLink(string code) => new()
        {
            Code = code,
            TargetUrl = command.TargetUrl,
            CreatedAtUtc = utcNow.UtcDateTime,
            ExpiresAtUtc = expiresAtUtc,
        };

        if (customCode is not null)
        {
            // RN-01: links nunca são apagados, então a checagem também pega código de link expirado.
            // D-04: o índice único cobre a corrida entre a checagem e a gravação.
            if (await links.CodeExistsAsync(customCode, cancellationToken))
                return CreateLinkResult.CodeInUse(CodeInUseError);

            var link = NewLink(customCode);
            return await links.TryAddAsync(link, cancellationToken)
                ? CreateLinkResult.Created(link)
                : CreateLinkResult.CodeInUse(CodeInUseError);
        }

        // D-11: cada pedido cria um link novo, sem procurar link existente para a mesma URL.
        // D-08: colisão detectada pelo índice único; gera outro código até o limite.
        for (var attempt = 0; attempt < MaxGenerationAttempts; attempt++)
        {
            var link = NewLink(codeGenerator.Generate());

            if (await links.TryAddAsync(link, cancellationToken))
                return CreateLinkResult.Created(link);
        }

        return CreateLinkResult.Unavailable(
            "Não foi possível gerar um código livre para o link agora. Tente novamente em instantes.");
    }

    public async Task<ResolveLinkResult> ResolveAsync(string code, CancellationToken cancellationToken)
    {
        // D-10: o código da URL é normalizado antes da busca.
        var link = await links.FindByCodeAsync(LinkCode.Normalize(code), cancellationToken);
        if (link is null)
            return ResolveLinkResult.NotFound();

        return ResolveLinkResult.Redirect(link.TargetUrl);
    }
}
