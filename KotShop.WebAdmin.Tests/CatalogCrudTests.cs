using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using KotShop.Application.Catalogo.Administrar;
using KotShop.Application.Abstractions;
using KotShop.Application.Produtos.ConsultarCatalogo;
using KotShop.Data.Persistence;
using KotShop.WebAdmin.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
using Bunit;
using KotShop.WebAdmin.Components.Pages;
using KotShop.Application.Produtos.AtualizarImagem;
using KotShop.WebAdmin.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace KotShop.WebAdmin.Tests;

public sealed class CatalogCrudTests
{
    [Fact]
    public async Task Admin_forms_create_edit_and_delete_the_catalog_used_by_the_storefront()
    {
        using var app = new CatalogApp();
        using var client = app.CreateClient(new() { AllowAutoRedirect = false });
        AssertRedirect(await PostForm(client, "/categorias/novo", "categoria-editar", new() { ["Input.Nome"] = "Livros" }));
        using var scope = app.Services.CreateScope();
        var admin = scope.ServiceProvider.GetRequiredService<ICatalogoAdminService>();
        var catalog = scope.ServiceProvider.GetRequiredService<ICatalogoService>();
        var category = Assert.Single(await admin.ListarCategoriasAsync(), c => c.Nome == "Livros");
        var values = new Dictionary<string, string>
        {
            ["Input.Nome"] = "Livro novo", ["Input.Preco"] = "49.90", ["Input.Descricao"] = "Descrição completa",
            ["Input.CategoriaId"] = category.Id.ToString(), ["Input.UrlImagem"] = "https://example.com/capa.png"
        };
        SaveProduct(app, null, values);
        var product = Assert.Single(await catalog.ListarAsync(), p => p.Nome == "Livro novo");
        Assert.Equal(49.90m, product.Preco);
        Assert.Equal("Livros", product.Categoria);

        values["Input.Nome"] = "Livro revisado";
        values["Input.Preco"] = "59.90";
        values["Input.UrlImagem"] = "";
        SaveProduct(app, product.Id, values);
        AssertRedirect(await PostForm(client, $"/categorias/{category.Id}/editar", "categoria-editar", new() { ["Input.Nome"] = "Livros e revistas" }));
        product = Assert.Single(await catalog.ListarAsync(), p => p.Id == product.Id);
        Assert.Equal("Livro revisado", product.Nome);
        Assert.Equal("Livros e revistas", product.Categoria);
        Assert.Equal(59.90m, product.Preco);
        Assert.Null(product.UrlImagem);
        Assert.Equal("Descrição completa", (await admin.ObterProdutoAsync(product.Id))!.Descricao);

        // GET confirmation never mutates data. POST rejects deleting a category still in use.
        var categoryPage = await client.GetAsync($"/categorias/{category.Id}/excluir");
        Assert.Equal(HttpStatusCode.OK, categoryPage.StatusCode);
        var blocked = await PostForm(client, $"/categorias/{category.Id}/excluir", "confirmar-exclusao", new());
        Assert.Equal(HttpStatusCode.OK, blocked.StatusCode);
        Assert.Contains("possui produtos", await blocked.Content.ReadAsStringAsync());
        Assert.Contains(await admin.ListarCategoriasAsync(), c => c.Id == category.Id);
        AssertRedirect(await PostForm(client, $"/produtos/{product.Id}/excluir", "confirmar-exclusao", new()));
        Assert.DoesNotContain(await catalog.ListarAsync(), p => p.Id == product.Id);
        AssertRedirect(await PostForm(client, $"/categorias/{category.Id}/excluir", "confirmar-exclusao", new()));
        Assert.DoesNotContain(await admin.ListarCategoriasAsync(), c => c.Id == category.Id);
    }

    [Theory]
    [InlineData("/produtos/novo", "produto-editar")]
    [InlineData("/produtos/1/editar", "produto-editar")]
    [InlineData("/produtos/1/excluir", "confirmar-exclusao")]
    [InlineData("/categorias/novo", "categoria-editar")]
    [InlineData("/categorias/1/editar", "categoria-editar")]
    [InlineData("/categorias/1/excluir", "confirmar-exclusao")]
    public async Task Mutation_forms_require_antiforgery(string path, string handler)
    {
        using var app = new CatalogApp();
        using var client = app.CreateClient(new() { AllowAutoRedirect = false });
        var response = await client.PostAsync(path, new FormUrlEncodedContent(new Dictionary<string, string> { ["_handler"] = handler }));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Invalid_product_data_is_rejected_without_changing_the_storefront()
    {
        using var app = new CatalogApp();
        using var scope = app.Services.CreateScope();
        var admin = scope.ServiceProvider.GetRequiredService<ICatalogoAdminService>();
        var invalid = new[]
        {
            new SalvarProduto("", 10, "Descrição", 1, null),
            new SalvarProduto("Produto", -1, "Descrição", 1, null),
            new SalvarProduto("Produto", 1.234m, "Descrição", 1, null),
            new SalvarProduto("Produto", 10, "Descrição", 9999, null),
            new SalvarProduto("Produto", 10, "Descrição", 1, "javascript:alert(1)")
        };
        foreach (var data in invalid)
            await Assert.ThrowsAsync<CatalogoAdminException>(() => admin.SalvarProdutoAsync(null, data));
        Assert.Equal(3, (await scope.ServiceProvider.GetRequiredService<ICatalogoService>().ListarAsync()).Count);
        await Assert.ThrowsAsync<CatalogoAdminException>(() => admin.SalvarProdutoAsync(9999, new("Produto", 10, "Descrição", 1, null)));
        await Assert.ThrowsAsync<CatalogoAdminException>(() => admin.SalvarCategoriaAsync(9999, "Nome"));
    }

    private static void AssertRedirect(HttpResponseMessage response) => Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

    private static void SaveProduct(CatalogApp app, long? id, Dictionary<string, string> values)
    {
        using var scope = app.Services.CreateScope();
        using var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddLogging();
        context.Services.AddSingleton(scope.ServiceProvider.GetRequiredService<ICatalogoAdminService>());
        context.Services.AddSingleton(scope.ServiceProvider.GetRequiredService<IProdutoImagemService>());
        var page = context.Render<ProdutoEditar>(p => p.Add(c => c.Id, id));
        Assert.False(page.Find("fieldset").HasAttribute("disabled"));
        page.Find("#nome").Change(values["Input.Nome"]);
        page.Find("#preco").Change(values["Input.Preco"]);
        page.Find("#descricao").Change(values["Input.Descricao"]);
        page.Find("#categoria").Change(values["Input.CategoriaId"]);
        page.FindAll("input[type=radio]")[1].Change(true);
        page.Find("#imagem-url").Input(values["Input.UrlImagem"]);
        page.Find("form").Submit();
        page.WaitForAssertion(() => Assert.Contains("Alteração salva", page.Markup));
        Assert.Equal("http://localhost/", context.Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>().Uri);
    }

    [Fact]
    public async Task Editor_upload_and_removal_preserve_the_other_fields_without_navigation()
    {
        using var app = new CatalogApp();
        using var scope = app.Services.CreateScope();
        using var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddLogging();
        var admin = scope.ServiceProvider.GetRequiredService<ICatalogoAdminService>();
        context.Services.AddSingleton(admin);
        context.Services.AddSingleton(scope.ServiceProvider.GetRequiredService<IProdutoImagemService>());
        var page = context.Render<ProdutoEditar>(p => p.Add(c => c.Id, 1));
        page.Find("#nome").Change("Nome preservado");
        page.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromBinary([137, 80, 78, 71, 13, 10, 26, 10, 0, 0], "nova.png"));
        page.WaitForAssertion(() => Assert.StartsWith("data:image/png;base64,", page.Find("img").GetAttribute("src")));
        page.Find("form").Submit();
        page.WaitForAssertion(() => Assert.Contains("Alteração salva", page.Markup));
        var saved = (await admin.ObterProdutoAsync(1))!;
        Assert.Equal("Nome preservado", saved.Nome);
        Assert.Equal(saved.UrlImagem, page.Find("img").GetAttribute("src"));
        page.FindComponent<ProdutoImagemCard>().Find("button").Click();
        Assert.Empty(page.FindAll("img"));
        Assert.Equal("Nome preservado", page.Find("#nome").GetAttribute("value"));
        page.Find("form").Submit();
        page.WaitForAssertion(() => Assert.Contains("Alteração salva", page.Markup));
        Assert.Null((await admin.ObterProdutoAsync(1))!.UrlImagem);
        Assert.Contains(saved.UrlImagem!, ((TestStorage)scope.ServiceProvider.GetRequiredService<IFileStorage>()).Removed);
        Assert.Equal("http://localhost/", context.Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>().Uri);
    }

    [Fact]
    public async Task Image_upload_uses_the_existing_service_and_updates_the_storefront()
    {
        using var app = new CatalogApp();
        using var client = app.CreateClient(new() { AllowAutoRedirect = false });
        var page = await client.GetStringAsync("/produtos/1/editar");
        var token = Regex.Match(page, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(WebUtility.HtmlDecode(token)), "__RequestVerificationToken");
        form.Add(new ByteArrayContent([137, 80, 78, 71, 13, 10, 26, 10, 0, 0]), "arquivo", "capa.png");
        var response = await client.PostAsync("/produtos/1/imagem", form);
        AssertRedirect(response);
        Assert.Contains("upload=sucesso", response.Headers.Location!.ToString());
        using var scope = app.Services.CreateScope();
        var product = Assert.Single(await scope.ServiceProvider.GetRequiredService<ICatalogoService>().ListarAsync(), p => p.Id == 1);
        Assert.StartsWith("https://storage.example.com/produtos/1/", product.UrlImagem);
        using var noToken = new MultipartFormDataContent();
        noToken.Add(new ByteArrayContent([1, 2, 3]), "arquivo", "file.png");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/produtos/1/imagem", noToken)).StatusCode);
    }

    private static async Task<HttpResponseMessage> PostForm(HttpClient client, string path, string handler, Dictionary<string, string> values)
    {
        var page = await client.GetStringAsync(path);
        var token = Regex.Match(page, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(token.Success, "O formulário deve conter um token antiforgery.");
        values["__RequestVerificationToken"] = WebUtility.HtmlDecode(token.Groups[1].Value);
        values["_handler"] = handler;
        return await client.PostAsync(path, new FormUrlEncodedContent(values));
    }

    private sealed class CatalogApp : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection connection = new("Data Source=:memory:");
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            connection.Open();
            var factory = new CatalogFactory(connection);
            using (var db = factory.CreateDbContext()) db.Database.EnsureCreated();
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ApplyMigrations"] = "false", ["HttpsRedirection:Enabled"] = "false"
            }));
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IDbContextFactory<CatalogoDbContext>>(factory);
                services.AddSingleton<IFileStorage, TestStorage>();
                services.AddAuthentication(options => options.DefaultAuthenticateScheme = "CatalogTest")
                    .AddScheme<AuthenticationSchemeOptions, MasterAuthentication>("CatalogTest", _ => { });
            });
        }
        protected override void Dispose(bool disposing) { base.Dispose(disposing); if (disposing) connection.Dispose(); }
    }

    private sealed class CatalogFactory(SqliteConnection connection) : IDbContextFactory<CatalogoDbContext>
    {
        public CatalogoDbContext CreateDbContext() => new(new DbContextOptionsBuilder<CatalogoDbContext>().UseSqlite(connection).Options);
    }
    private sealed class TestStorage : IFileStorage
    {
        public List<string> Removed { get; } = [];
        public Task DeleteOwnedImageAsync(string url, long produtoId, CancellationToken cancellationToken = default)
        {
            Removed.Add(url);
            return Task.CompletedTask;
        }
        public Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default) =>
            Task.FromResult($"https://storage.example.com/{key}");
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
    private sealed class MasterAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(AuthenticateResult.Success(
            new(new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "master-test"), new Claim("permissao", AdminAccess.MasterPermission)], Scheme.Name)), Scheme.Name)));
    }
}

