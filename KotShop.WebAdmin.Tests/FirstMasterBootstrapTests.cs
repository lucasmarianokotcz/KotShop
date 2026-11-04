using KotShop.Data.Identity;
using KotShop.WebAdmin.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KotShop.WebAdmin.Tests;

public sealed class FirstMasterBootstrapTests
{
    private const string StrongPassword = "Senha-de-teste!2026";

    [Fact]
    public async Task Creates_confirmed_master_with_hashed_password_and_catalog_access()
    {
        await using var fixture = await Fixture.CreateAsync();
        var result = await fixture.Bootstrap.CreateAsync(" master@example.com ", StrongPassword);
        Assert.True(result.Succeeded);
        var user = await fixture.Users.FindByEmailAsync("master@example.com");
        Assert.NotNull(user);
        Assert.True(user.EmailConfirmed);
        Assert.NotEqual(StrongPassword, user.PasswordHash);
        Assert.True(await fixture.Users.CheckPasswordAsync(user, StrongPassword));
        var claims = await fixture.Users.GetClaimsAsync(user);
        Assert.Contains(claims, c => c.Type == "permissao" && c.Value == AdminAccess.MasterPermission);
        Assert.Contains(claims, c => c.Type == "permissao" && c.Value == AdminAccess.CatalogPermission);
        Assert.True(await fixture.Bootstrap.HasMasterAsync());
    }

    [Fact]
    public async Task Second_bootstrap_cannot_create_another_master_or_reset_the_first_password()
    {
        await using var fixture = await Fixture.CreateAsync();
        Assert.True((await fixture.Bootstrap.CreateAsync("master@example.com", StrongPassword)).Succeeded);
        var second = await fixture.Bootstrap.CreateAsync("other@example.com", StrongPassword);
        var reset = await fixture.Bootstrap.CreateAsync("master@example.com", "Outra-senha!2026");
        Assert.Contains(second.Errors, e => e.Code == "MasterAlreadyExists");
        Assert.Contains(reset.Errors, e => e.Code == "MasterAlreadyExists");
        Assert.Equal(1, await fixture.Database.Users.CountAsync());
        Assert.True(await fixture.Users.CheckPasswordAsync((await fixture.Users.FindByEmailAsync("master@example.com"))!, StrongPassword));
    }

    [Fact]
    public async Task Existing_customer_is_not_promoted_or_confirmed()
    {
        await using var fixture = await Fixture.CreateAsync();
        var customer = new ApplicationUser { UserName = "customer@example.com", Email = "customer@example.com" };
        Assert.True((await fixture.Users.CreateAsync(customer, StrongPassword)).Succeeded);
        var result = await fixture.Bootstrap.CreateAsync("CUSTOMER@example.com", StrongPassword);
        Assert.Contains(result.Errors, e => e.Code == "AccountAlreadyExists");
        Assert.Empty(await fixture.Users.GetClaimsAsync(customer));
        Assert.False(customer.EmailConfirmed);
        Assert.False(await fixture.Bootstrap.HasMasterAsync());
    }

    [Theory]
    [InlineData("invalid", StrongPassword)]
    [InlineData("master@example.com", "Short!1")]
    [InlineData("master@example.com", "alllowercasepassword")]
    public async Task Invalid_input_does_not_leave_an_account(string email, string password)
    {
        await using var fixture = await Fixture.CreateAsync();
        Assert.False((await fixture.Bootstrap.CreateAsync(email, password)).Succeeded);
        Assert.Empty(await fixture.Database.Users.ToListAsync());
        Assert.Empty(await fixture.Database.UserClaims.ToListAsync());
    }

    [Fact]
    public async Task Failure_when_granting_permissions_rolls_back_the_new_account()
    {
        await using var fixture = await Fixture.CreateAsync(rejectClaimUpdate: true);
        var result = await fixture.Bootstrap.CreateAsync("master@example.com", StrongPassword);
        Assert.Contains(result.Errors, e => e.Code == "RejectedUpdate");
        Assert.Empty(await fixture.Database.Users.AsNoTracking().ToListAsync());
        Assert.Empty(await fixture.Database.UserClaims.AsNoTracking().ToListAsync());
    }

    private sealed class RejectSecondValidation : IUserValidator<ApplicationUser>
    {
        private int calls;
        public Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user) =>
            Task.FromResult(++calls == 1 ? IdentityResult.Success : IdentityResult.Failed(
                new IdentityError { Code = "RejectedUpdate", Description = "Falha simulada na concessão de permissões." }));
    }

    private sealed class Fixture(SqliteConnection connection, ServiceProvider provider, AsyncServiceScope scope) : IAsyncDisposable
    {
        public ApplicationDbContext Database => scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        public UserManager<ApplicationUser> Users => scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        public FirstMasterBootstrap Bootstrap => scope.ServiceProvider.GetRequiredService<FirstMasterBootstrap>();

        public static async Task<Fixture> CreateAsync(bool rejectClaimUpdate = false)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));
            services.AddIdentityCore<ApplicationUser>(options => options.Stores.SchemaVersion = IdentitySchemaVersions.Version3)
                .AddEntityFrameworkStores<ApplicationDbContext>();
            services.AddScoped<FirstMasterBootstrap>();
            if (rejectClaimUpdate) services.AddScoped<IUserValidator<ApplicationUser>, RejectSecondValidation>();
            var provider = services.BuildServiceProvider();
            var fixture = new Fixture(connection, provider, provider.CreateAsyncScope());
            await fixture.Database.Database.EnsureCreatedAsync();
            return fixture;
        }

        public async ValueTask DisposeAsync()
        {
            await scope.DisposeAsync();
            await provider.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
