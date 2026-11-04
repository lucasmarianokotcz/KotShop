using KotShop.Application.Produtos;
using KotShop.Application.Produtos.AtualizarImagem;
using KotShop.WebAdmin.Security;
using Microsoft.AspNetCore.Mvc;

namespace KotShop.WebAdmin.Features;

public static class ProdutoImagemEndpoints
{
    public static void MapAdminProdutoImagemEndpoints(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/produtos/{id:long}/imagem", UploadAsync)
            .RequireAuthorization(AdminAccess.Policy)
            .WithMetadata(new RequestSizeLimitAttribute(ProdutoImagemService.TamanhoMaximoImagem + 64 * 1024));

    private static async Task<IResult> UploadAsync(long id, IFormFile arquivo, IProdutoImagemService service,
        ILoggerFactory loggerFactory, CancellationToken cancellationToken)
    {
        string status;
        try
        {
            await using var stream = arquivo.OpenReadStream();
            await service.AtualizarAsync(id, stream, cancellationToken);
            status = "sucesso";
        }
        catch (ImagemInvalidaException) { status = "invalida"; }
        catch (ProdutoNaoEncontradoException) { status = "nao-encontrado"; }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            loggerFactory.CreateLogger("AdminProdutoImagem").LogError(exception, "Falha ao enviar imagem do produto {ProdutoId}.", id);
            status = "falha";
        }
        return Results.LocalRedirect($"/produtos/{id}/editar?upload={status}");
    }
}
