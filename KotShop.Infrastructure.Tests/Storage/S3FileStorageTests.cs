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

        public override Task<PutObjectResponse> PutObjectAsync(PutObjectRequest request, CancellationToken cancellationToken = default)
        {
            if (FailUpload) throw new AmazonS3Exception("Upload failed");
            Upload = request;
            return Task.FromResult(new PutObjectResponse());
        }
    }
}
