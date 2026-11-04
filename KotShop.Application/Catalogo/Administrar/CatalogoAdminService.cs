using KotShop.Domain.Categorias;
using KotShop.Domain.Produtos;

namespace KotShop.Application.Catalogo.Administrar;

public sealed class CatalogoAdminService(ICatalogoAdminRepository repository) : ICatalogoAdminService
{
    public Task<IReadOnlyList<CategoriaAdmin>> ListarCategoriasAsync(CancellationToken cancellationToken = default) =>
        repository.ListarCategoriasAsync(cancellationToken);
    public Task<ProdutoAdmin?> ObterProdutoAsync(long id, CancellationToken cancellationToken = default) =>
        repository.ObterProdutoAsync(id, cancellationToken);
    public Task ExcluirProdutoAsync(long id, CancellationToken cancellationToken = default) =>
        repository.ExcluirProdutoAsync(id, cancellationToken);
    public Task ExcluirCategoriaAsync(long id, CancellationToken cancellationToken = default) =>
        repository.ExcluirCategoriaAsync(id, cancellationToken);

    public Task<long> SalvarProdutoAsync(long? id, SalvarProduto dados, CancellationToken cancellationToken = default)
    {
        if (dados.Preco > 9999999999999999.99m || decimal.Round(dados.Preco, 2) != dados.Preco)
            throw new CatalogoAdminException("Informe um preço com até duas casas decimais, dentro do limite permitido.");
        try
        {
            var produto = new Produto(dados.Nome, dados.Preco, dados.Descricao, dados.CategoriaId);
            produto.DefinirImagem(string.IsNullOrWhiteSpace(dados.UrlImagem) ? null : dados.UrlImagem.Trim());
            return repository.SalvarProdutoAsync(id, produto, cancellationToken);
        }
        catch (ArgumentException exception) { throw new CatalogoAdminException(exception.Message); }
    }

    public Task<long> SalvarCategoriaAsync(long? id, string nome, CancellationToken cancellationToken = default)
    {
        try { return repository.SalvarCategoriaAsync(id, new Categoria(nome), cancellationToken); }
        catch (ArgumentException exception) { throw new CatalogoAdminException(exception.Message); }
    }
}
