using KotShop.Application.Produtos.ConsultarCatalogo;
using KotShop.Domain.Produtos;

namespace KotShop.Application.Produtos;

public interface IProdutoRepository
{
    Task<IReadOnlyList<ProdutoResumo>> ListarAsync(CancellationToken cancellationToken = default);
    Task<Produto?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default);
    Task SalvarImagemAsync(long id, string? urlImagem, CancellationToken cancellationToken = default);
}
