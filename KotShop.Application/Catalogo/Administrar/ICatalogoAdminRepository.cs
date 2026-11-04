using KotShop.Domain.Categorias;
using KotShop.Domain.Produtos;

namespace KotShop.Application.Catalogo.Administrar;

public interface ICatalogoAdminRepository
{
    Task<IReadOnlyList<CategoriaAdmin>> ListarCategoriasAsync(CancellationToken cancellationToken);
    Task<ProdutoAdmin?> ObterProdutoAsync(long id, CancellationToken cancellationToken);
    Task<long> SalvarProdutoAsync(long? id, Produto produto, CancellationToken cancellationToken);
    Task ExcluirProdutoAsync(long id, CancellationToken cancellationToken);
    Task<long> SalvarCategoriaAsync(long? id, Categoria categoria, CancellationToken cancellationToken);
    Task ExcluirCategoriaAsync(long id, CancellationToken cancellationToken);
}
