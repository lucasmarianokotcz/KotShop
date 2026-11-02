using System.ComponentModel.DataAnnotations;

namespace KotShop.Web.Storage;

public sealed class S3StorageOptions
{
    [Required, Url] public string ServiceUrl { get; set; } = "";
    [Required, Url] public string PublicBaseUrl { get; set; } = "";
    [Required] public string Region { get; set; } = "";
    [Required] public string BucketName { get; set; } = "";
    [Required] public string AccessKey { get; set; } = "";
    [Required] public string SecretKey { get; set; } = "";
}
