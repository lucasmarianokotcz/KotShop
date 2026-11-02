using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using KotShop.Web.Data;
using KotShop.Web.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace KotShop.Tests;

public class InfrastructureTests
{
    private const string Connection = "Host=localhost;Database=kotshop;Username=kotshop;Password=kotshop";

    [Fact]
    public void CatalogMigrationUsesPostgresAndAdvancesSeededSequences()
    {
        using var db = new DataContext(new DbContextOptionsBuilder<DataContext>().UseNpgsql(Connection).Options);
        Assert.False(db.Database.HasPendingModelChanges());
        var sql = db.GetService<IMigrator>().GenerateScript();
        Assert.Contains("numeric(18,2)", sql);
        Assert.Contains("\"UrlImagem\" character varying(1000)", sql);
        Assert.Contains("setval", sql);
        Assert.Contains("Smartphone", sql);
        Assert.DoesNotContain("nvarchar", sql);
    }

    [Fact]
    public void IdentityMigrationIncludesPostgresPasskeys()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(Connection));
        services.AddIdentityCore<ApplicationUser>(options => options.Stores.SchemaVersion = IdentitySchemaVersions.Version3)
            .AddEntityFrameworkStores<ApplicationDbContext>();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(db.Database.HasPendingModelChanges());
        var sql = db.GetService<IMigrator>().GenerateScript();
        Assert.Contains("\"AspNetUserPasskeys\"", sql);
        Assert.Contains("jsonb", sql);
        Assert.Contains("bytea", sql);
        Assert.DoesNotContain("[NormalizedName]", sql);
    }

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
