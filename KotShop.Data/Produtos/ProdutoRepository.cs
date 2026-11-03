using KotShop.Application.Produtos;
using KotShop.Application.Produtos.ConsultarCatalogo;
using KotShop.Data.Persistence;
using KotShop.Domain.Produtos;
using Microsoft.EntityFrameworkCore;

namespace KotShop.Data.Produtos;

public sealed class ProdutoRepository(IDbContextFactory<CatalogoDbContext> factory) : IProdutoRepository
{
    public async Task<IReadOnlyList<ProdutoResumo>> ListarAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.Produtos.AsNoTracking().OrderBy(p => p.Id)
            .Select(p => new ProdutoResumo(p.Id, p.Nome, p.Preco, p.Categoria.Nome, p.UrlImagem))
            .ToListAsync(cancellationToken);
    }

    public async Task<Produto?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.Produtos.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task SalvarImagemAsync(long id, string? urlImagem, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        // Update only the image; do not overwrite concurrent edits to the rest of the product.
        var affected = await db.Produtos.Where(p => p.Id == id)
            .ExecuteUpdateAsync(update => update.SetProperty(p => p.UrlImagem, urlImagem), cancellationToken);
        if (affected == 0)
            throw new ProdutoNaoEncontradoException(id);
    }
}
