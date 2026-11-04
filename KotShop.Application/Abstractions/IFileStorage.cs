namespace KotShop.Application.Abstractions;

public interface IFileStorage
{
    Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default);
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
    Task DeleteOwnedImageAsync(string url, long produtoId, CancellationToken cancellationToken = default);
}
