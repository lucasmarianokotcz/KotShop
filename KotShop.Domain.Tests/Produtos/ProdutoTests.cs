using KotShop.Domain.Produtos;
using Xunit;

namespace KotShop.Domain.Tests.Produtos;

public class ProdutoTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void RejeitaNomeVazio(string? nome) =>
        Assert.ThrowsAny<ArgumentException>(() => new Produto(nome!, 10m, "Descrição", 1));

    [Fact]
    public void RejeitaNomeMaiorQueOArmazenamentoSuporta() =>
        Assert.Throws<ArgumentException>(() => new Produto(new string('a', 256), 10m, "Descrição", 1));

    [Fact]
    public void RejeitaPrecoNegativo() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new Produto("Produto", -0.01m, "Descrição", 1));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ExigeCategoriaValida(long categoriaId) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new Produto("Produto", 10m, "Descrição", categoriaId));

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:image/png;base64,abc")]
    [InlineData("file:///tmp/imagem.png")]
    [InlineData("/imagem.png")]
    [InlineData("")]
    public void UrlInvalidaNaoSubstituiImagemExistente(string url)
    {
        var produto = new Produto("Produto", 10m, "Descrição", 1);
        produto.DefinirImagem("https://cdn.example.com/anterior.png");

        Assert.Throws<ArgumentException>(() => produto.DefinirImagem(url));

        Assert.Equal("https://cdn.example.com/anterior.png", produto.UrlImagem);
    }

    [Fact]
    public void RejeitaUrlQueUltrapassaColunaDoBanco()
    {
        var produto = new Produto("Produto", 10m, "Descrição", 1);
        Assert.Throws<ArgumentException>(() => produto.DefinirImagem("https://example.com/" + new string('a', 1000)));
        Assert.Null(produto.UrlImagem);
    }

    [Fact]
    public void PermiteRemoverImagemSemInvalidarProduto()
    {
        var produto = new Produto("Produto", 10m, "Descrição", 1);
        produto.DefinirImagem("http://localhost:4566/kotshop-arquivos/produtos/1/a.png");
        produto.DefinirImagem(null);
        Assert.Null(produto.UrlImagem);
    }
}
