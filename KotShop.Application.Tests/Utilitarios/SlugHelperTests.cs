using KotShop.Application.Utilitarios;
using Xunit;

namespace KotShop.Application.Tests.Utilitarios;

public sealed class SlugHelperTests
{
    [Theory]
    [InlineData("Informática", "informatica")]
    [InlineData("Eletrônicos", "eletronicos")]
    [InlineData("Máquinas de Lavar", "maquinas-de-lavar")]
    [InlineData("  Café   & AÇÚCAR!  ", "cafe-acucar")]
    [InlineData("Ma\u0301quinas-de--Lavar", "maquinas-de-lavar")]
    [InlineData("123 / ABC", "123-abc")]
    [InlineData("---", "")]
    [InlineData("", "")]
    public void CriarNormalizaTexto(string texto, string esperado)
    {
        Assert.Equal(esperado, SlugHelper.Criar(texto));
    }

    [Fact]
    public void CriarRejeitaTextoNulo()
    {
        Assert.Throws<ArgumentNullException>(() => SlugHelper.Criar(null!));
    }
}
