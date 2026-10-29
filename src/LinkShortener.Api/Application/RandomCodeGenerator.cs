using System.Security.Cryptography;

namespace LinkShortener.Api.Application;

public sealed class RandomCodeGenerator : ICodeGenerator
{
    public string Generate() =>
        RandomNumberGenerator.GetString(LinkCode.Alphabet, LinkCode.GeneratedLength);
}
