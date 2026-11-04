using KotShop.Application.Produtos.AtualizarImagem;
using KotShop.Application.Produtos.ConsultarCatalogo;
using Microsoft.Extensions.DependencyInjection;

namespace KotShop.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICatalogoService, CatalogoService>();
        services.AddScoped<Catalogo.Administrar.ICatalogoAdminService, Catalogo.Administrar.CatalogoAdminService>();
        services.AddScoped<IProdutoImagemService, ProdutoImagemService>();
        return services;
    }
}
