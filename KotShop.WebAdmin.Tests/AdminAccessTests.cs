using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using KotShop.Application.Produtos.ConsultarCatalogo;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
using KotShop.WebAdmin.Security;

namespace KotShop.WebAdmin.Tests;

public sealed class AdminAccessTests
{
    [Theory]
    [InlineData("/")]
    [InlineData("/produtos")]
    [InlineData("/produtos/novo")]
    [InlineData("/produtos/1/editar")]
    [InlineData("/produtos/1/excluir")]
    [InlineData("/categorias")]
    [InlineData("/categorias/novo")]
    [InlineData("/categorias/1/editar")]
    [InlineData("/categorias/1/excluir")]
    public async Task Anonymous_is_redirected_to_admin_login(string path)
    {
        using var factory = new AdminFactory();
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location!.ToString());
    }

    [Theory]
    [InlineData("/Account/Login")]
    [InlineData("/Account/LoginWith2fa")]
    [InlineData("/health")]
    public async Task Public_routes_are_accessible_without_database(string path)
    {
        using var factory = new AdminFactory();
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(path)).StatusCode);
    }

    [Theory]
    [InlineData("/", false, HttpStatusCode.Forbidden)]
    [InlineData("/produtos", false, HttpStatusCode.Forbidden)]
    [InlineData("/produtos/novo", false, HttpStatusCode.Forbidden)]
    [InlineData("/produtos/1/editar", false, HttpStatusCode.Forbidden)]
    [InlineData("/produtos/1/excluir", false, HttpStatusCode.Forbidden)]
    [InlineData("/categorias", false, HttpStatusCode.Forbidden)]
    [InlineData("/categorias/novo", false, HttpStatusCode.Forbidden)]
    [InlineData("/categorias/1/editar", false, HttpStatusCode.Forbidden)]
    [InlineData("/categorias/1/excluir", false, HttpStatusCode.Forbidden)]
    [InlineData("/", true, HttpStatusCode.OK)]
    [InlineData("/produtos", true, HttpStatusCode.OK)]
    public async Task Admin_claim_is_required(string path, bool admin, HttpStatusCode expected)
    {
        using var factory = new AdminFactory(authenticated: true, admin: admin);
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var response = await client.GetAsync(path);
        Assert.Equal(expected, response.StatusCode);
        if (admin && path == "/produtos") Assert.Contains("Produto de teste", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/produtos")]
    public async Task Master_has_access_without_an_individual_catalog_claim(string path)
    {
        using var factory = new AdminFactory(authenticated: true, master: true);
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(path)).StatusCode);
    }

    [Fact]
    public async Task Logout_requires_antiforgery_token()
    {
        using var factory = new AdminFactory(authenticated: true, admin: true);
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/Account/Logout", new FormUrlEncodedContent([]))).StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Visitors_and_customers_cannot_post_catalog_changes(bool authenticated)
    {
        using var factory = new AdminFactory(authenticated: authenticated);
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        foreach (var path in new[] { "/produtos/novo", "/produtos/1/editar", "/produtos/1/excluir", "/produtos/1/imagem", "/categorias/novo", "/categorias/1/editar", "/categorias/1/excluir" })
        {
            var response = await client.PostAsync(path, new FormUrlEncodedContent([]));
            Assert.Equal(authenticated ? HttpStatusCode.Forbidden : HttpStatusCode.Redirect, response.StatusCode);
        }
    }

    [Fact]
    public async Task Admin_does_not_expose_public_registration()
    {
        using var factory = new AdminFactory(authenticated: true, admin: true);
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/Account/Register")).StatusCode);
    }

    private sealed class AdminFactory(bool authenticated = false, bool admin = false, bool master = false) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ApplyMigrations"] = "false",
                ["HttpsRedirection:Enabled"] = "false"
            }));
            builder.ConfigureServices(services =>
            {
                services.AddScoped<ICatalogoService, FakeCatalogo>();
                if (authenticated)
                {
                    services.AddAuthentication(options => options.DefaultAuthenticateScheme = "Test")
                        .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", _ => { });
                    services.Configure<AuthenticationOptions>(options => options.DefaultForbidScheme = "Test");
                    services.AddSingleton(new TestUser(admin, master));
                }
            });
        }
    }

    private sealed record TestUser(bool Admin, bool Master);
    private sealed class TestAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
        UrlEncoder encoder, TestUser user) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "test"), new(ClaimTypes.Name, "test@example.com") };
            if (user.Admin) claims.Add(new("permissao", "catalogo:escrever"));
            if (user.Master) claims.Add(new("permissao", AdminAccess.MasterPermission));
            return Task.FromResult(AuthenticateResult.Success(new(new(new ClaimsIdentity(claims, Scheme.Name)), Scheme.Name)));
        }
    }

    private sealed class FakeCatalogo : ICatalogoService
    {
        public Task<IReadOnlyList<ProdutoResumo>> ListarAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ProdutoResumo>>([new(1, "Produto de teste", 10, "Categoria", null)]);
    }
}
