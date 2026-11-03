namespace KotShop.Application.Produtos.ConsultarCatalogo;

public sealed class CatalogoService(IProdutoRepository produtos) : ICatalogoService
{
    public Task<IReadOnlyList<ProdutoResumo>> ListarAsync(CancellationToken cancellationToken = default) =>
        produtos.ListarAsync(cancellationToken);
}
