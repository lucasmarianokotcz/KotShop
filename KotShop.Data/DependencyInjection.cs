using KotShop.Application.Produtos;
using KotShop.Data.Identity;
using KotShop.Data.Persistence;
using KotShop.Data.Produtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KotShop.Data;

public static class DependencyInjection
{
    public static IServiceCollection AddData(this IServiceCollection services, IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connection));
        // A factory gives each catalog operation its own context, including Blazor server circuits.
        services.AddDbContextFactory<CatalogoDbContext>(options => options.UseNpgsql(connection));
        services.AddScoped<IProdutoRepository, ProdutoRepository>();
        services.AddScoped<KotShop.Application.Catalogo.Administrar.ICatalogoAdminRepository, Catalogo.CatalogoAdminRepository>();
        return services;
    }

    public static async Task ApplyDatabaseMigrationsAsync(this IServiceProvider provider, CancellationToken cancellationToken = default)
    {
        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync(cancellationToken);
        await scope.ServiceProvider.GetRequiredService<CatalogoDbContext>().Database.MigrateAsync(cancellationToken);
    }
}
