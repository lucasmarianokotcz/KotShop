using KotShop.Data.Identity;
using KotShop.Data.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace KotShop.Infrastructure.Tests.Persistence;

public class MigrationTests
{
    private const string Connection = "Host=localhost;Database=kotshop;Username=kotshop;Password=kotshop";

    [Fact]
    public void CatalogMigrationUsesPostgresAndAdvancesSeededSequences()
    {
        using var db = new CatalogoDbContext(new DbContextOptionsBuilder<CatalogoDbContext>().UseNpgsql(Connection).Options);
        Assert.False(db.Database.HasPendingModelChanges());
        var sql = db.GetService<IMigrator>().GenerateScript();
        Assert.Contains("numeric(18,2)", sql);
        Assert.Contains("\"UrlImagem\" character varying(1000)", sql);
        Assert.Contains("setval", sql);
        Assert.Contains("Smartphone", sql);
        Assert.DoesNotContain("nvarchar", sql);
    }

    [Fact]
    public void IdentityMigrationIncludesPostgresPasskeys()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(Connection));
        services.AddIdentityCore<ApplicationUser>(options => options.Stores.SchemaVersion = IdentitySchemaVersions.Version3)
            .AddEntityFrameworkStores<ApplicationDbContext>();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(db.Database.HasPendingModelChanges());
        var sql = db.GetService<IMigrator>().GenerateScript();
        Assert.Contains("\"AspNetUserPasskeys\"", sql);
        Assert.Contains("jsonb", sql);
        Assert.Contains("bytea", sql);
        Assert.DoesNotContain("[NormalizedName]", sql);
    }

}
