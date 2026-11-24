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
using MainLayout = Store::KotShop.Web.Components.Layout.MainLayout;
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
    public void FiltraPorCategoriaEAtualizaQuandoQueryMuda()
    {
        using var context = CriarContexto();
        var navegacao = context.Services.GetRequiredService<NavigationManager>();
        navegacao.NavigateTo("/produtos?categoria=cafe");
        var pagina = context.Render<Produtos>(p => p.AddCascadingValue(Categorias));

        Assert.Single(pagina.FindAll("article"));
        Assert.Equal("Produto Café", pagina.Find("article h2").TextContent);
        Assert.Contains("Produtos - Café", pagina.Markup);

        navegacao.NavigateTo("/produtos?categoria=informatica");
        pagina.WaitForAssertion(() =>
        {
            Assert.Single(pagina.FindAll("article"));
            Assert.Equal("Computador", pagina.Find("article h2").TextContent);
            Assert.Contains("Produtos - Informática", pagina.Markup);
        });

        navegacao.NavigateTo("/produtos");
        pagina.WaitForAssertion(() => Assert.Equal(3, pagina.FindAll("article").Count));
    }

    [Fact]
    public void LinksDeCategoriaUsamApenasSlugEDestacamCategoriaAtual()
    {
        using var context = CriarContexto();
        context.Services.GetRequiredService<NavigationManager>().NavigateTo("/produtos?categoria=informatica");
        var layout = context.Render<MainLayout>();
        var links = layout.FindAll("section[aria-label=Categorias] a");

        Assert.Equal(4, links.Count);
        Assert.Equal("/produtos?categoria=cafe", links[0].GetAttribute("href"));
        Assert.Equal("/produtos?categoria=cafe", links[1].GetAttribute("href"));
        Assert.Equal("/produtos?categoria=informatica", links[3].GetAttribute("href"));
        Assert.Equal("font-bold", links[3].GetAttribute("class"));
        Assert.DoesNotContain("font-bold", links[0].GetAttribute("class") ?? string.Empty);
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
    [InlineData("/produtos?categoria=")]
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
        context.Services.AddLogging();
        context.Services.AddSingleton<ICatalogoService>(new CatalogoFake());
        context.Services.AddSingleton<ICategoriaService>(new CategoriaFake());
        return context;
    }

    private sealed class CategoriaFake : ICategoriaService
    {
        public Task<IReadOnlyList<CategoriaResumo>> ListarAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Categorias);
    }

    private sealed class CatalogoFake : ICatalogoService
    {
        public Task<IReadOnlyList<ProdutoResumo>> ListarProdutosAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(ProdutosCatalogo);
    }
}
