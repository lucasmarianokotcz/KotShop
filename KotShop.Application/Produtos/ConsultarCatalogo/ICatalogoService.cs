namespace KotShop.Application.Produtos.ConsultarCatalogo;

public interface ICatalogoService
{
    Task<IReadOnlyList<ProdutoResumo>> ListarAsync(CancellationToken cancellationToken = default);
}
