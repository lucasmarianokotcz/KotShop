using KotShop.Application.Utilitarios;

namespace KotShop.Application.Categorias;

public static class CategoriaSlug
{
    public static CategoriaResumo? Encontrar(IReadOnlyList<CategoriaResumo> categorias, string valor)
    {
        var slug = SlugHelper.Criar(valor);

        if (slug.Length == 0)
        {
            return null;
        }

        return categorias.FirstOrDefault(categoria => categoria.Nome == valor)
            ?? categorias.FirstOrDefault(categoria => SlugHelper.Criar(categoria.Nome) == slug);
    }
}
