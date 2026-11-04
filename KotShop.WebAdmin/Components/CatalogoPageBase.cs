using KotShop.Application.Catalogo.Administrar;
using Microsoft.AspNetCore.Components;

namespace KotShop.WebAdmin.Components;

public abstract class CatalogoPageBase : ComponentBase
{
    protected string? Erro { get; private set; }
    [Inject] private ILogger<CatalogoPageBase> Logger { get; set; } = default!;
    protected async Task<bool> ExecutarAsync(Func<Task> operation)
    {
        Erro = null;
        try { await operation(); return true; }
        catch (CatalogoAdminException exception) { Erro = exception.Message; }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Falha na administração do catálogo.");
            Erro = "Não foi possível concluir a operação. Tente novamente em instantes.";
        }
        return false;
    }
}
