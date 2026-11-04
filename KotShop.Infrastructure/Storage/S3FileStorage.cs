using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using KotShop.Application.Abstractions;

namespace KotShop.Infrastructure.Storage;

public sealed class S3FileStorage(IAmazonS3 s3, IOptions<S3StorageOptions> options) : IFileStorage
{
    private readonly S3StorageOptions storage = options.Value;

    public async Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        await s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = storage.BucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false
        }, cancellationToken);

        var escapedKey = string.Join('/', key.Split('/').Select(Uri.EscapeDataString));
        return $"{storage.PublicBaseUrl.TrimEnd('/')}/{storage.BucketName}/{escapedKey}";
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default) =>
        s3.DeleteObjectAsync(storage.BucketName, key, cancellationToken);

    public Task DeleteOwnedImageAsync(string url, long produtoId, CancellationToken cancellationToken = default)
    {
        var prefix = $"{storage.PublicBaseUrl.TrimEnd('/')}/{storage.BucketName}/produtos/{produtoId}/";
        if (!url.StartsWith(prefix, StringComparison.Ordinal)) return Task.CompletedTask;
        var file = url[prefix.Length..].Split('.');
        if (file.Length != 2 || !Guid.TryParseExact(file[0], "N", out _) || file[1] is not ("png" or "jpg" or "webp"))
            return Task.CompletedTask;
        return DeleteAsync($"produtos/{produtoId}/{file[0]}.{file[1]}", cancellationToken);
    }
}
