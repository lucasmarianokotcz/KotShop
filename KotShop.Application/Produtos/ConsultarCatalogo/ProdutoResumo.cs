namespace KotShop.Application.Produtos.ConsultarCatalogo;

public sealed record ProdutoResumo(long Id, string Nome, decimal Preco, string Categoria, string? UrlImagem);
