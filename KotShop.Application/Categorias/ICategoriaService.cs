namespace KotShop.Application.Categorias;

public interface ICategoriaService
{
    Task<IReadOnlyList<CategoriaResumo>> ListarAsync(CancellationToken cancellationToken = default);
}
