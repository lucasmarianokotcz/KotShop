using Flowbite.Services;
using KotShop.Application;
using KotShop.Data;
using KotShop.Data.Identity;
using KotShop.Infrastructure;
using KotShop.WebAdmin.Components;
using KotShop.WebAdmin.Components.Account;
using KotShop.WebAdmin.Security;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Antiforgery;
using KotShop.WebAdmin.Features;

var bootstrapMaster = args.Contains("--bootstrap-master", StringComparer.Ordinal);
var builder = WebApplication.CreateBuilder(args.Where(arg => arg != "--bootstrap-master").ToArray());
builder.Services.AddFlowbite();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
builder.Services.AddDataProtection().SetApplicationName("KotShop.WebAdmin");
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
}).AddIdentityCookies(cookies =>
{
    cookies.ApplicationCookie!.Configure(options =>
    {
        options.Cookie.Name = ".KotShop.WebAdmin.Identity";
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });
    cookies.TwoFactorUserIdCookie!.Configure(options => options.Cookie.Name = ".KotShop.WebAdmin.TwoFactorUserId");
    cookies.TwoFactorRememberMeCookie!.Configure(options => options.Cookie.Name = ".KotShop.WebAdmin.TwoFactorRememberMe");
    cookies.ExternalCookie!.Configure(options => options.Cookie.Name = ".KotShop.WebAdmin.External");
});
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AdminAccess.Policy, AdminAccess.Configure)
    .AddPolicy(AdminAccess.MasterPolicy, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permissao", AdminAccess.MasterPermission))
    .SetFallbackPolicy(AdminAccess.CreatePolicy());
builder.Services.AddApplication();
builder.Services.AddData(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
}).AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager().AddDefaultTokenProviders();
builder.Services.AddScoped<FirstMasterBootstrap>();

var app = builder.Build();
if (bootstrapMaster)
{
    Environment.ExitCode = await MasterBootstrapCommand.RunAsync(app.Services);
    return;
}
if (app.Configuration.GetValue<bool>("Database:ApplyMigrations"))
    await app.Services.ApplyDatabaseMigrationsAsync();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
// Re-executing a rejected form POST would read an already invalid antiforgery form again.
app.UseWhen(context => HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method),
    branch => branch.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true));
if (app.Configuration.GetValue("HttpsRedirection:Enabled", true))
    app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapStaticAssets().AllowAnonymous();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapAdminProdutoImagemEndpoints();
app.MapPost("/Account/Logout", async (HttpContext context, IAntiforgery antiforgery, SignInManager<ApplicationUser> signInManager) =>
{
    try { await antiforgery.ValidateRequestAsync(context); }
    catch (AntiforgeryValidationException) { return Results.BadRequest(); }
    await signInManager.SignOutAsync();
    return Results.LocalRedirect("/Account/Login");
}).RequireAuthorization(AdminAccess.Policy);
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.Run();

public partial class Program;
