using KotShop.Application.Categorias;
using Xunit;

namespace KotShop.Application.Tests.Categorias;

public sealed class CategoriaSlugTests
{
    private static readonly IReadOnlyList<CategoriaResumo> Categorias =
        [new(1, "Café"), new(2, "Cafe"), new(3, "Informática"), new(4, "東京")];

    [Fact]
    public void IdentificadorEUsadoApenasParaSlugAmbiguoOuVazio()
    {
        Assert.True(CategoriaSlug.PrecisaIdentificador(Categorias[0], Categorias));
        Assert.True(CategoriaSlug.PrecisaIdentificador(Categorias[1], Categorias));
        Assert.False(CategoriaSlug.PrecisaIdentificador(Categorias[2], Categorias));
        Assert.True(CategoriaSlug.PrecisaIdentificador(Categorias[3], Categorias));
    }

    [Fact]
    public void EncontrarDistingueCategoriasComMesmoSlugEValidaIdentificador()
    {
        Assert.Equal(Categorias[0], CategoriaSlug.Encontrar(Categorias, "cafe", 1));
        Assert.Equal(Categorias[1], CategoriaSlug.Encontrar(Categorias, "cafe", 2));
        Assert.Null(CategoriaSlug.Encontrar(Categorias, "informatica", 1));
        Assert.Equal(Categorias[3], CategoriaSlug.Encontrar(Categorias, "", 4));
    }

    [Fact]
    public void EncontrarAceitaLinksAntigosEValorNormalizado()
    {
        Assert.Equal(Categorias[0], CategoriaSlug.Encontrar(Categorias, "Café", null));
        Assert.Equal(Categorias[1], CategoriaSlug.Encontrar(Categorias, "Cafe", null));
        Assert.Equal(Categorias[2], CategoriaSlug.Encontrar(Categorias, "informatica", null));
        Assert.Null(CategoriaSlug.Encontrar(Categorias, "", null));
        Assert.Null(CategoriaSlug.Encontrar(Categorias, "desconhecida", null));
    }
}
