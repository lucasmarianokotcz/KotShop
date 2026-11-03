namespace KotShop.Application.Produtos.AtualizarImagem;

public interface IProdutoImagemService
{
    Task<ImagemProdutoAtualizada> AtualizarAsync(long produtoId, Stream imagem, CancellationToken cancellationToken = default);
}

public sealed record ImagemProdutoAtualizada(long Id, string UrlImagem);
