namespace LinkShortener.Api.Application;

public sealed class LinkService(
    ILinkRepository links,
    ICodeGenerator codeGenerator,
    PublicHostChecker hostChecker,
    TimeProvider timeProvider)
{
    public async Task<CreateLinkResult> CreateAsync(CreateLinkCommand command, CancellationToken cancellationToken)
    {
        if (!TargetUrlRules.TryValidate(command.TargetUrl, out var targetUri, out var urlError))
            return CreateLinkResult.Invalid(urlError);

        if (!await hostChecker.IsPubliclyAccessibleAsync(targetUri, cancellationToken))
            return CreateLinkResult.Invalid(
                "A URL de destino precisa ser publicamente acessível: o host deve existir e não pode apontar para a rede interna, para a própria máquina nem para endereços reservados.");

        // D-11: cada pedido cria um link novo, sem procurar link existente para a mesma URL.
        var link = new Link
        {
            Code = codeGenerator.Generate(),
            TargetUrl = command.TargetUrl,
            CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime,
        };

        if (!await links.TryAddAsync(link, cancellationToken))
            return CreateLinkResult.Unavailable("Não foi possível gerar um código para o link. Tente novamente.");

        return CreateLinkResult.Created(link);
    }
}
