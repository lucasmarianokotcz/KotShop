using Amazon.Runtime;
using Amazon.S3;
using KotShop.Application.Abstractions;
using KotShop.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace KotShop.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<S3StorageOptions>().Bind(configuration.GetSection("Storage"))
            .ValidateDataAnnotations().ValidateOnStart();
        services.AddSingleton<IAmazonS3>(provider =>
        {
            var storage = provider.GetRequiredService<IOptions<S3StorageOptions>>().Value;
            return new AmazonS3Client(new BasicAWSCredentials(storage.AccessKey, storage.SecretKey), new AmazonS3Config
            {
                ServiceURL = storage.ServiceUrl,
                AuthenticationRegion = storage.Region,
                ForcePathStyle = true
            });
        });
        services.AddScoped<IFileStorage, S3FileStorage>();
        return services;
    }
}
