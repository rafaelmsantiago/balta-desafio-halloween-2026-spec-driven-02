using LinkShortener.Api.Application;

namespace LinkShortener.Tests.Fakes;

// Devolve os códigos na ordem dada e repete o último quando acabam.
public sealed class SequenceCodeGenerator(params string[] codes) : ICodeGenerator
{
    private int _next;

    public int Calls => _next;

    public string Generate() => codes[Math.Min(_next++, codes.Length - 1)];
}
