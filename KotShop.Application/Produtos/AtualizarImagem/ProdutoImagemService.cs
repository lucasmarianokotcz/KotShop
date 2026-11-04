using KotShop.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace KotShop.Application.Produtos.AtualizarImagem;

public sealed class ProdutoImagemService(
    IProdutoRepository produtos,
    IFileStorage storage,
    ILogger<ProdutoImagemService> logger) : IProdutoImagemService
{
    private const int TamanhoMaximoImagemEmMb = 5;
    public const int TamanhoMaximoImagem = TamanhoMaximoImagemEmMb * 1024 * 1024;
    private const int TamanhoBufferLeituraEmKb = 80;
    private const int TamanhoBufferLeitura = TamanhoBufferLeituraEmKb * 1024;

    public Task RemoverArquivoAsync(long produtoId, string url, CancellationToken cancellationToken = default) =>
        storage.DeleteOwnedImageAsync(url, produtoId, cancellationToken);

    public async Task<ImagemProdutoAtualizada> AtualizarAsync(long produtoId, Stream imagem, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(imagem);
        cancellationToken.ThrowIfCancellationRequested();
        await using var content = await LerImagemAsync(imagem, cancellationToken);
        var (extensao, contentType) = DetectarFormato(content.GetBuffer().AsSpan(0, (int)content.Length));

        var produto = await produtos.ObterPorIdAsync(produtoId, cancellationToken)
            ?? throw new ProdutoNaoEncontradoException(produtoId);
        var key = $"produtos/{produtoId}/{Guid.NewGuid():N}.{extensao}";
        var url = await storage.UploadAsync(key, content, contentType, cancellationToken);

        try
        {
            // Validate the storage response before persisting a URL in the catalog.
            ArgumentException.ThrowIfNullOrWhiteSpace(url);
            produto.DefinirImagem(url);
            await produtos.SalvarImagemAsync(produtoId, produto.UrlImagem, cancellationToken);
        }
        catch (Exception)
        {
            // Compensation must still run when the original request has been cancelled.
            try
            {
                await storage.DeleteAsync(key, CancellationToken.None);
            }
            catch (Exception cleanupError)
            {
                // Keep the persistence error; report the object requiring cleanup separately.
                logger.LogError(cleanupError, "Falha ao remover o objeto {ObjectKey} após falha na atualização do produto {ProdutoId}.", key, produtoId);
            }
            throw;
        }

        return new ImagemProdutoAtualizada(produtoId, url);
    }

    private static async Task<MemoryStream> LerImagemAsync(Stream imagem, CancellationToken cancellationToken)
    {
        var content = new MemoryStream();
        try
        {
            // Read at most limit + 1 bytes, including for non-seekable streams and dishonest clients.
            var buffer = new byte[TamanhoBufferLeitura];
            int read;
            while ((read = await imagem.ReadAsync(buffer.AsMemory(0, (int)Math.Min(buffer.Length,
                TamanhoMaximoImagem + 1 - content.Length)), cancellationToken)) > 0)
            {
                await content.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                if (content.Length > TamanhoMaximoImagem)
                    throw new ImagemInvalidaException($"Envie uma imagem de até {TamanhoMaximoImagemEmMb} MB.");
            }
            if (content.Length == 0)
                throw new ImagemInvalidaException($"Envie uma imagem de até {TamanhoMaximoImagemEmMb} MB.");
            content.Position = 0;
            return content;
        }
        catch
        {
            await content.DisposeAsync();
            throw;
        }
    }

    public static (string Extensao, string ContentType) DetectarFormato(ReadOnlySpan<byte> bytes)
    {
        if (bytes.StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
            return ("png", "image/png");
        if (bytes.StartsWith(new byte[] { 255, 216, 255 }))
            return ("jpg", "image/jpeg");
        if (bytes.Length >= 12 && bytes[..4].SequenceEqual("RIFF"u8) && bytes.Slice(8, 4).SequenceEqual("WEBP"u8))
            return ("webp", "image/webp");
        throw new ImagemInvalidaException("Formatos aceitos: PNG, JPEG e WebP.");
    }
}
