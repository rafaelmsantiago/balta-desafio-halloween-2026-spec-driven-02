namespace LinkShortener.Api.Application;

public sealed class LinkService(
    ILinkRepository links,
    ICodeGenerator codeGenerator,
    PublicHostChecker hostChecker,
    TimeProvider timeProvider)
{
    public const int MaxGenerationAttempts = 5;

    public async Task<CreateLinkResult> CreateAsync(CreateLinkCommand command, CancellationToken cancellationToken)
    {
        if (!TargetUrlRules.TryValidate(command.TargetUrl, out var targetUri, out var urlError))
            return CreateLinkResult.Invalid(urlError);

        var utcNow = timeProvider.GetUtcNow();
        if (!LinkExpiration.TryGetExpiresAtUtc(command.ExpiresOn, utcNow, out var expiresAtUtc, out var expirationError))
            return CreateLinkResult.Invalid(expirationError);

        if (!await hostChecker.IsPubliclyAccessibleAsync(targetUri, cancellationToken))
            return CreateLinkResult.Invalid(
                "A URL de destino precisa ser publicamente acessível: o host deve existir e não pode apontar para a rede interna, para a própria máquina nem para endereços reservados.");

        // D-11: cada pedido cria um link novo, sem procurar link existente para a mesma URL.
        // D-08: colisão detectada pelo índice único; gera outro código até o limite.
        for (var attempt = 0; attempt < MaxGenerationAttempts; attempt++)
        {
            var link = new Link
            {
                Code = codeGenerator.Generate(),
                TargetUrl = command.TargetUrl,
                CreatedAtUtc = utcNow.UtcDateTime,
                ExpiresAtUtc = expiresAtUtc,
            };

            if (await links.TryAddAsync(link, cancellationToken))
                return CreateLinkResult.Created(link);
        }

        return CreateLinkResult.Unavailable(
            "Não foi possível gerar um código livre para o link agora. Tente novamente em instantes.");
    }
}
