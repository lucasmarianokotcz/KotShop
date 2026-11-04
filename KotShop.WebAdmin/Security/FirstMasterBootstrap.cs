using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using KotShop.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KotShop.WebAdmin.Security;

public sealed class FirstMasterBootstrap(ApplicationDbContext database, UserManager<ApplicationUser> users)
{
    public Task<bool> HasMasterAsync(CancellationToken cancellationToken = default) =>
        database.UserClaims.AnyAsync(c => c.ClaimType == "permissao" && c.ClaimValue == AdminAccess.MasterPermission,
            cancellationToken);

    public async Task<IdentityResult> CreateAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        email = email.Trim();
        if (!new EmailAddressAttribute().IsValid(email))
            return Failure("InvalidEmail", "Informe um e-mail válido.");
        if (password.Length < 12)
            return Failure("PasswordTooShort", "A senha do primeiro administrador deve ter pelo menos 12 caracteres.");

        // UserManager and the Identity store share this scoped context. A failure rolls back both user and claims.
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        if (database.Database.IsNpgsql())
        {
            // Serialize bootstrap attempts, including attempts using different emails.
            await database.Database.ExecuteSqlRawAsync("LOCK TABLE \"AspNetUsers\" IN EXCLUSIVE MODE", cancellationToken);
        }
        if (await HasMasterAsync(cancellationToken))
            return Failure("MasterAlreadyExists", "Já existe um administrador master. Nenhuma conta foi alterada.");
        if (await users.FindByEmailAsync(email) is not null || await users.FindByNameAsync(email) is not null)
            return Failure("AccountAlreadyExists", "Esse e-mail já pertence a uma conta. Use um e-mail ainda não cadastrado; este comando não promove contas existentes.");

        // This is a trusted operator command, so the initial account does not depend on the public email flow.
        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        var result = await users.CreateAsync(user, password);
        if (!result.Succeeded) return result;
        result = await users.AddClaimsAsync(user,
        [
            new Claim("permissao", AdminAccess.MasterPermission),
            // Also authorizes existing catalog endpoints in the storefront.
            new Claim("permissao", AdminAccess.CatalogPermission)
        ]);
        if (!result.Succeeded) return result;
        await transaction.CommitAsync(cancellationToken);
        return IdentityResult.Success;
    }

    private static IdentityResult Failure(string code, string description) =>
        IdentityResult.Failed(new IdentityError { Code = code, Description = description });
}
