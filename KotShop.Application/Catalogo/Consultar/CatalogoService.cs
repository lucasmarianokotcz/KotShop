using KotShop.Application.Produtos;
using KotShop.Application.Produtos.ConsultarCatalogo;

namespace KotShop.Application.Catalogo.Consultar;

public sealed class CatalogoService(IProdutoRepository produtos) : ICatalogoService
{
    public Task<IReadOnlyList<ProdutoResumo>> ListarProdutosAsync(CancellationToken cancellationToken = default) =>
        produtos.ListarAsync(cancellationToken);
}
