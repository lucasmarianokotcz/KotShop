using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace KotShop.Web.Storage;

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
}
