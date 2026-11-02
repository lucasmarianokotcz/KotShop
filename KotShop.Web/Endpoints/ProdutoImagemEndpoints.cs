using KotShop.Web.Data;
using KotShop.Web.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KotShop.Web.Endpoints;

public static class ProdutoImagemEndpoints
{
    private const long MaxImageSize = 5 * 1024 * 1024;

    public static void MapProdutoImagemEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/produtos/{id:long}/imagem", UploadAsync)
            .RequireAuthorization("AdministrarCatalogo")
            .WithMetadata(new RequestSizeLimitAttribute(MaxImageSize + 64 * 1024));
    }

    private static async Task<IResult> UploadAsync(long id, IFormFile arquivo,
        IDbContextFactory<DataContext> factory, IFileStorage storage, CancellationToken cancellationToken)
    {
        if (arquivo.Length is <= 0 or > MaxImageSize)
            return Results.BadRequest("Envie uma imagem de até 5 MB.");

        // Validate bytes instead of trusting the supplied file name or Content-Type.
        await using var content = new MemoryStream();
        await arquivo.CopyToAsync(content, cancellationToken);
        var imageType = DetectImageType(content.ToArray());
        if (imageType is null)
            return Results.BadRequest("Formatos aceitos: PNG, JPEG e WebP.");

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var produto = await db.Produtos.FindAsync([id], cancellationToken);
        if (produto is null)
            return Results.NotFound();

        var (extension, contentType) = imageType.Value;
        var key = $"produtos/{id}/{Guid.NewGuid():N}.{extension}";
        content.Position = 0;
        produto.UrlImagem = await storage.UploadAsync(key, content, contentType, cancellationToken);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Do not leave an orphan object if saving the product fails.
            await storage.DeleteAsync(key, CancellationToken.None);
            throw;
        }

        return Results.Ok(new { produto.Id, produto.UrlImagem });
    }

    internal static (string Extension, string ContentType)? DetectImageType(byte[] bytes)
    {
        if (bytes.AsSpan().StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
            return ("png", "image/png");
        if (bytes.AsSpan().StartsWith(new byte[] { 255, 216, 255 }))
            return ("jpg", "image/jpeg");
        if (bytes.Length >= 12 && bytes.AsSpan(0, 4).SequenceEqual("RIFF"u8)
            && bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8))
            return ("webp", "image/webp");
        return null;
    }
}
