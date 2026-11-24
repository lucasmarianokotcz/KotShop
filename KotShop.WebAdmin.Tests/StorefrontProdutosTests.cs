extern alias Store;

using Bunit;
using Flowbite.Services;
using KotShop.Application.Catalogo.Consultar;
using KotShop.Application.Categorias;
using KotShop.Application.Produtos.ConsultarCatalogo;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Home = Store::KotShop.Web.Features.Produtos.Home;
using Produtos = Store::KotShop.Web.Components.Pages.Produtos;

namespace KotShop.WebAdmin.Tests;

public sealed class StorefrontProdutosTests
{
    private static readonly IReadOnlyList<CategoriaResumo> Categorias =
        [new(1, "Café"), new(2, "Cafe"), new(3, "Vazia"), new(4, "Informática")];

    private static readonly IReadOnlyList<ProdutoResumo> ProdutosCatalogo =
    [
        new(1, "Produto Café", 100m, "Café", "https://example.com/cafe.png", 1),
        new(2, "Produto Cafe", 200m, "Cafe", null, 2),
        new(3, "Computador", 300m, "Informática", null, 4)
    ];

    [Fact]
    public void SemFiltroExibeMesmosProdutosEApresentacaoDaPaginaInicial()
    {
        using var context = CriarContexto();
        var home = context.Render<Home>();
        var pagina = context.Render<Produtos>(p => p.AddCascadingValue(Categorias));

        Assert.Equal(home.Find("main").OuterHtml, pagina.Find("main").OuterHtml);
        Assert.Equal(3, pagina.FindAll("article").Count);
        Assert.Equal("https://example.com/cafe.png", pagina.Find("img").GetAttribute("src"));
    }

    [Fact]
    public void FiltraPorIdentificadorEAtualizaQuandoQueryMuda()
    {
        using var context = CriarContexto();
        var navegacao = context.Services.GetRequiredService<NavigationManager>();
        navegacao.NavigateTo("/produtos?categoria=cafe&categoriaId=1");
        var pagina = context.Render<Produtos>(p => p.AddCascadingValue(Categorias));

        Assert.Contains("Produto Café", pagina.Markup);
        Assert.DoesNotContain("Produto Cafe</h2>", pagina.Markup);
        Assert.Contains("Produtos - Café", pagina.Markup);

        navegacao.NavigateTo("/produtos?categoria=cafe&categoriaId=2");
        pagina.WaitForAssertion(() =>
        {
            Assert.Contains("Produto Cafe", pagina.Markup);
            Assert.DoesNotContain("Produto Café</h2>", pagina.Markup);
            Assert.Contains("Produtos - Cafe", pagina.Markup);
        });

        navegacao.NavigateTo("/produtos");
        pagina.WaitForAssertion(() => Assert.Equal(3, pagina.FindAll("article").Count));
    }

    [Theory]
    [InlineData("/produtos?categoria=informatica")]
    [InlineData("/produtos?categoria=Inform%C3%A1tica")]
    public void FiltraPorSlugOuNomeOriginal(string url)
    {
        using var context = CriarContexto();
        context.Services.GetRequiredService<NavigationManager>().NavigateTo(url);
        var pagina = context.Render<Produtos>(p => p.AddCascadingValue(Categorias));

        Assert.Single(pagina.FindAll("article"));
        Assert.Equal("Computador", pagina.Find("article h2").TextContent);
        Assert.Equal("Produtos - Informática", pagina.Find("h3").TextContent);
    }

    [Theory]
    [InlineData("/produtos?categoria=vazia")]
    [InlineData("/produtos?categoria=desconhecida")]
    [InlineData("/produtos?categoria=cafe&categoriaId=99")]
    public void CategoriaVaziaOuDesconhecidaNaoExibeOutrosProdutos(string url)
    {
        using var context = CriarContexto();
        context.Services.GetRequiredService<NavigationManager>().NavigateTo(url);
        var pagina = context.Render<Produtos>(p => p.AddCascadingValue(Categorias));

        Assert.Empty(pagina.FindAll("article"));
        Assert.Contains("Nenhum produto disponível.", pagina.Markup);
    }

    private static BunitContext CriarContexto()
    {
        var context = new BunitContext();
        context.Services.AddFlowbite();
        context.Services.AddSingleton<ICatalogoService>(new CatalogoFake());
        return context;
    }

    private sealed class CatalogoFake : ICatalogoService
    {
        public Task<IReadOnlyList<ProdutoResumo>> ListarProdutosAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(ProdutosCatalogo);
    }
}
