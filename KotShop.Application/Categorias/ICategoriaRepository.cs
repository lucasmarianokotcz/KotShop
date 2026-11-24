namespace KotShop.Application.Categorias;

public interface ICategoriaRepository
{
    Task<IReadOnlyList<CategoriaResumo>> ListarAsync(CancellationToken cancellationToken = default);
}
