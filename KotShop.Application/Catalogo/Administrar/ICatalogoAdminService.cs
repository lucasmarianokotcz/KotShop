namespace KotShop.Application.Catalogo.Administrar;

public sealed record CategoriaAdmin(long Id, string Nome, int QuantidadeProdutos);
public sealed record ProdutoAdmin(long Id, string Nome, decimal Preco, string Descricao, long CategoriaId, string? UrlImagem);
public sealed record SalvarProduto(string Nome, decimal Preco, string Descricao, long CategoriaId, string? UrlImagem);

public sealed class CatalogoAdminException(string message) : Exception(message);

public interface ICatalogoAdminService
{
    Task<IReadOnlyList<CategoriaAdmin>> ListarCategoriasAsync(CancellationToken cancellationToken = default);
    Task<ProdutoAdmin?> ObterProdutoAsync(long id, CancellationToken cancellationToken = default);
    Task<long> SalvarProdutoAsync(long? id, SalvarProduto dados, CancellationToken cancellationToken = default);
    Task ExcluirProdutoAsync(long id, CancellationToken cancellationToken = default);
    Task<long> SalvarCategoriaAsync(long? id, string nome, CancellationToken cancellationToken = default);
    Task ExcluirCategoriaAsync(long id, CancellationToken cancellationToken = default);
}
