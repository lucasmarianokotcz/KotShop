using KotShop.Application.Utilitarios;

namespace KotShop.Application.Categorias;

public static class CategoriaSlug
{
    public static bool PrecisaIdentificador(CategoriaResumo categoria, IReadOnlyList<CategoriaResumo> categorias)
    {
        var slug = SlugHelper.Criar(categoria.Nome);
        return slug.Length == 0 || categorias.Count(item => SlugHelper.Criar(item.Nome) == slug) > 1;
    }

    public static CategoriaResumo? Encontrar(IReadOnlyList<CategoriaResumo> categorias, string valor, long? id)
    {
        var slug = SlugHelper.Criar(valor);
        if (id.HasValue)
        {
            return categorias.FirstOrDefault(categoria => categoria.Id == id && SlugHelper.Criar(categoria.Nome) == slug);
        }

        if (slug.Length == 0)
        {
            return null;
        }

        return categorias.FirstOrDefault(categoria => categoria.Nome == valor)
            ?? categorias.FirstOrDefault(categoria => SlugHelper.Criar(categoria.Nome) == slug);
    }
}
