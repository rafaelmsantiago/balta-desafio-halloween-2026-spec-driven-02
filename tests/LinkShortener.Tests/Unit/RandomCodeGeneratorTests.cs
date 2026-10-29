using LinkShortener.Api.Application;

namespace LinkShortener.Tests.Unit;

public class RandomCodeGeneratorTests
{
    private readonly RandomCodeGenerator _generator = new();

    [Fact]
    public void Generate_devolve_7_caracteres()
    {
        Assert.Equal(7, _generator.Generate().Length);
    }

    [Fact]
    public void Generate_usa_somente_A_a_Z_maiusculo_e_1_a_9()
    {
        for (var i = 0; i < 1000; i++)
        {
            var code = _generator.Generate();

            Assert.Matches("^[A-Z1-9]{7}$", code);
        }
    }

    [Fact]
    public void Alfabeto_nao_tem_o_digito_0()
    {
        Assert.Equal(35, LinkCode.Alphabet.Length);
        Assert.DoesNotContain('0', LinkCode.Alphabet);
    }
}
