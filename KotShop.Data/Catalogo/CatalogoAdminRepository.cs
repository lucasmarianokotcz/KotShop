using KotShop.Application.Catalogo.Administrar;
using KotShop.Data.Persistence;
using KotShop.Domain.Categorias;
using KotShop.Domain.Produtos;
using Microsoft.EntityFrameworkCore;

namespace KotShop.Data.Catalogo;

public sealed class CatalogoAdminRepository(IDbContextFactory<CatalogoDbContext> factory) : ICatalogoAdminRepository
{
    public async Task<IReadOnlyList<CategoriaAdmin>> ListarCategoriasAsync(CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.Categorias.AsNoTracking().OrderBy(c => c.Nome)
            .Select(c => new CategoriaAdmin(c.Id, c.Nome, c.Produtos.Count)).ToListAsync(cancellationToken);
    }

    public async Task<ProdutoAdmin?> ObterProdutoAsync(long id, CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.Produtos.AsNoTracking().Where(p => p.Id == id)
            .Select(p => new ProdutoAdmin(p.Id, p.Nome, p.Preco, p.Descricao, p.CategoriaId, p.UrlImagem))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<long> SalvarProdutoAsync(long? id, Produto dados, CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        if (!await db.Categorias.AnyAsync(c => c.Id == dados.CategoriaId, cancellationToken))
            throw new CatalogoAdminException("A categoria selecionada não existe mais. Selecione outra categoria.");
        var produto = dados;
        if (id is not null)
        {
            produto = await db.Produtos.FindAsync([id.Value], cancellationToken)
                ?? throw new CatalogoAdminException("O produto não existe mais.");
            produto.Atualizar(dados.Nome, dados.Preco, dados.Descricao, dados.CategoriaId);
            produto.DefinirImagem(dados.UrlImagem);
        }
        else db.Produtos.Add(produto);
        await SalvarAsync(db, "Não foi possível salvar o produto. Atualize a página e confira a categoria selecionada.", cancellationToken);
        return produto.Id;
    }

    public async Task ExcluirProdutoAsync(long id, CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        if (await db.Produtos.Where(p => p.Id == id).ExecuteDeleteAsync(cancellationToken) == 0)
            throw new CatalogoAdminException("O produto não existe mais.");
    }

    public async Task<long> SalvarCategoriaAsync(long? id, Categoria dados, CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var categoria = dados;
        if (id is not null)
        {
            categoria = await db.Categorias.FindAsync([id.Value], cancellationToken)
                ?? throw new CatalogoAdminException("A categoria não existe mais.");
            categoria.Renomear(dados.Nome);
        }
        else db.Categorias.Add(categoria);
        await SalvarAsync(db, "Não foi possível salvar a categoria. Atualize a página e tente novamente.", cancellationToken);
        return categoria.Id;
    }

    public async Task ExcluirCategoriaAsync(long id, CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        if (await db.Produtos.AnyAsync(p => p.CategoriaId == id, cancellationToken))
            throw new CatalogoAdminException("Esta categoria possui produtos. Mova ou exclua esses produtos antes de excluir a categoria.");
        var categoria = await db.Categorias.FindAsync([id], cancellationToken)
            ?? throw new CatalogoAdminException("A categoria não existe mais.");
        db.Categorias.Remove(categoria);
        // The existing restrictive foreign key also protects a product added after the check above.
        await SalvarAsync(db, "Não foi possível excluir a categoria. Ela pode ter recebido produtos; atualize a página.", cancellationToken);
    }

    private static async Task SalvarAsync(CatalogoDbContext db, string message, CancellationToken cancellationToken)
    {
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException) { throw new CatalogoAdminException(message); }
    }
}
