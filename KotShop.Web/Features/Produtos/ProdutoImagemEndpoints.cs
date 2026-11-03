using KotShop.Application.Produtos;
using KotShop.Application.Produtos.AtualizarImagem;
using Microsoft.AspNetCore.Mvc;

namespace KotShop.Web.Features.Produtos;

public static class ProdutoImagemEndpoints
{
    public static void MapProdutoImagemEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/produtos/{id:long}/imagem", UploadAsync)
            .RequireAuthorization("AdministrarCatalogo")
            .WithMetadata(new RequestSizeLimitAttribute(ProdutoImagemService.TamanhoMaximoImagem + 64 * 1024));
    }

    private static async Task<IResult> UploadAsync(long id, IFormFile arquivo,
        IProdutoImagemService service, CancellationToken cancellationToken)
    {
        try
        {
            await using var content = arquivo.OpenReadStream();
            return Results.Ok(await service.AtualizarAsync(id, content, cancellationToken));
        }
        catch (ImagemInvalidaException error)
        {
            return Results.BadRequest(error.Message);
        }
        catch (ProdutoNaoEncontradoException)
        {
            return Results.NotFound();
        }
    }
}
