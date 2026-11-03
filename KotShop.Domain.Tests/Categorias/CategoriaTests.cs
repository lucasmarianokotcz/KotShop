using KotShop.Domain.Categorias;
using Xunit;

namespace KotShop.Domain.Tests.Categorias;

public class CategoriaTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NaoPermiteCategoriaSemNome(string? nome) =>
        Assert.ThrowsAny<ArgumentException>(() => new Categoria(nome!));

    [Fact]
    public void RejeitaNomeQueNaoCabeNaColunaDoBanco() =>
        Assert.Throws<ArgumentException>(() => new Categoria(new string('a', 256)));
}
