using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using KotShop.Infrastructure.Storage;
using Microsoft.Extensions.Options;
using Xunit;

namespace KotShop.Infrastructure.Tests.Storage;

public class S3FileStorageTests
{
    [Fact]
    public async Task UploadUsesInternalS3AndReturnsBrowserAccessibleEscapedUrl()
    {
        using var s3 = new RecordingS3();
        var storage = new S3FileStorage(s3, Options.Create(new S3StorageOptions
        {
            BucketName = "kotshop-arquivos",
            ServiceUrl = "http://localstack:4566",
            PublicBaseUrl = "http://localhost:4566/"
        }));
        await using var content = new MemoryStream([1, 2, 3]);
        var url = await storage.UploadAsync("produtos/1/imagem teste.png", content, "image/png");
        Assert.Equal("http://localhost:4566/kotshop-arquivos/produtos/1/imagem%20teste.png", url);
        Assert.Equal("kotshop-arquivos", s3.Upload!.BucketName);
        Assert.Equal("produtos/1/imagem teste.png", s3.Upload.Key);
        Assert.Equal("image/png", s3.Upload.ContentType);
        Assert.False(s3.Upload.AutoCloseStream);
    }

    [Fact]
    public async Task UploadPropagatesFailureInsteadOfReturningAnUnstoredUrl()
    {
        using var s3 = new RecordingS3 { FailUpload = true };
        var storage = new S3FileStorage(s3, Options.Create(new S3StorageOptions()));
        await using var content = new MemoryStream([1]);
        await Assert.ThrowsAsync<AmazonS3Exception>(() => storage.UploadAsync("produtos/1/a.png", content, "image/png"));
    }

    private sealed class RecordingS3() : AmazonS3Client(new BasicAWSCredentials("test", "test"),
        new AmazonS3Config { ServiceURL = "http://localstack:4566", ForcePathStyle = true })
    {
        public PutObjectRequest? Upload { get; private set; }
        public bool FailUpload { get; init; }
        public string? DeletedKey { get; private set; }
        public override Task<DeleteObjectResponse> DeleteObjectAsync(string bucketName, string key, CancellationToken cancellationToken = default)
        {
            DeletedKey = key;
            return Task.FromResult(new DeleteObjectResponse());
        }

        public override Task<PutObjectResponse> PutObjectAsync(PutObjectRequest request, CancellationToken cancellationToken = default)
        {
            if (FailUpload) throw new AmazonS3Exception("Upload failed");
            Upload = request;
            return Task.FromResult(new PutObjectResponse());
        }
    }

    [Theory]
    [InlineData("http://localhost:4566/kotshop-arquivos/produtos/1/00112233445566778899aabbccddeeff.png", true)]
    [InlineData("https://externo.com/produtos/1/00112233445566778899aabbccddeeff.png", false)]
    [InlineData("http://localhost:4566/kotshop-arquivos/produtos/2/00112233445566778899aabbccddeeff.png", false)]
    [InlineData("http://localhost:4566/kotshop-arquivos/produtos/1/../outro.png", false)]
    [InlineData("http://localhost:4566/kotshop-arquivos/produtos/1/%2e%2e%2foutro.png", false)]
    public async Task Removal_only_deletes_generated_images_owned_by_the_product(string url, bool shouldDelete)
    {
        using var s3 = new RecordingS3();
        var storage = new S3FileStorage(s3, Options.Create(new S3StorageOptions { BucketName = "kotshop-arquivos", PublicBaseUrl = "http://localhost:4566" }));
        await storage.DeleteOwnedImageAsync(url, 1);
        Assert.Equal(shouldDelete, s3.DeletedKey is not null);
    }
}
