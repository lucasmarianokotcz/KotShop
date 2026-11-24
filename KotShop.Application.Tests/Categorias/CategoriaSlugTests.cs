using KotShop.Application.Categorias;
using Xunit;

namespace KotShop.Application.Tests.Categorias;

public sealed class CategoriaSlugTests
{
    private static readonly IReadOnlyList<CategoriaResumo> Categorias =
        [new(1, "Café"), new(2, "Cafe"), new(3, "Informática"), new(4, "東京")];

    [Fact]
    public void EncontrarUsaPrimeiraCategoriaQuandoSlugsCoincidem()
    {
        Assert.Equal(Categorias[0], CategoriaSlug.Encontrar(Categorias, "cafe"));
    }

    [Fact]
    public void EncontrarAceitaLinksAntigosEValorNormalizado()
    {
        Assert.Equal(Categorias[0], CategoriaSlug.Encontrar(Categorias, "Café"));
        Assert.Equal(Categorias[1], CategoriaSlug.Encontrar(Categorias, "Cafe"));
        Assert.Equal(Categorias[2], CategoriaSlug.Encontrar(Categorias, "informatica"));
        Assert.Null(CategoriaSlug.Encontrar(Categorias, ""));
        Assert.Null(CategoriaSlug.Encontrar(Categorias, "desconhecida"));
    }
}
