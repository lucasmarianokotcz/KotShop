using KotShop.Application.Catalogo.Consultar;
using KotShop.Application.Categorias;
using KotShop.Application.Produtos.AtualizarImagem;
using Microsoft.Extensions.DependencyInjection;

namespace KotShop.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICatalogoService, CatalogoService>();
        services.AddScoped<ICategoriaService, CategoriaService>();
        services.AddScoped<Catalogo.Administrar.ICatalogoAdminService, Catalogo.Administrar.CatalogoAdminService>();
        services.AddScoped<IProdutoImagemService, ProdutoImagemService>();
        return services;
    }
}
