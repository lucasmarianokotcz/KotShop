using KotShop.Application.Categorias;
using KotShop.Data.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KotShop.Data.Categorias;

public sealed class CategoriaRepository(IDbContextFactory<CatalogoDbContext> factory) : ICategoriaRepository
{
    public async Task<IReadOnlyList<CategoriaResumo>> ListarAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.Categorias.AsNoTracking().OrderBy(c => c.Nome).ThenBy(c => c.Id)
            .Select(c => new CategoriaResumo(c.Id, c.Nome))
            .ToListAsync(cancellationToken);
    }
}
