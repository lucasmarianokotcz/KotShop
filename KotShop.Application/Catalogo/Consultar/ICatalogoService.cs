using KotShop.Application.Produtos.ConsultarCatalogo;

namespace KotShop.Application.Catalogo.Consultar;

public interface ICatalogoService
{
    Task<IReadOnlyList<ProdutoResumo>> ListarProdutosAsync(CancellationToken cancellationToken = default);
}
