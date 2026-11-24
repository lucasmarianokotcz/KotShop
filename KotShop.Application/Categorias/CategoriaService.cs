namespace KotShop.Application.Categorias;

public sealed class CategoriaService(ICategoriaRepository categorias) : ICategoriaService
{
    public Task<IReadOnlyList<CategoriaResumo>> ListarAsync(CancellationToken cancellationToken = default) =>
        categorias.ListarAsync(cancellationToken);
}
