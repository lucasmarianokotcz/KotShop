using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace KotShop.WebAdmin.Security;

public static class AdminAccess
{
    public const string Policy = "AdministrarCatalogo";
    public const string MasterPolicy = "AdministradorMaster";
    public const string MasterPermission = "administracao:total";
    public const string CatalogPermission = "catalogo:escrever";

    public static bool CanAccess(IEnumerable<Claim> claims) =>
        claims.Any(c => c.Type == "permissao" && (c.Value == MasterPermission || c.Value == CatalogPermission));

    public static void Configure(AuthorizationPolicyBuilder policy) =>
        policy.RequireAuthenticatedUser().RequireAssertion(context => CanAccess(context.User.Claims));

    public static AuthorizationPolicy CreatePolicy()
    {
        var builder = new AuthorizationPolicyBuilder();
        Configure(builder);
        return builder.Build();
    }
}
