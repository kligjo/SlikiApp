namespace UmbracoCms.Web.Infrastructure.Configuration.Options;

public sealed class BlobStorageOptions
{
    public string? ConnectionString { get; set; }

    public string ServiceUri { get; set; } = string.Empty;

    public string ContainerName { get; set; } = "sliki";

    public long MaxUploadBytes { get; set; } = 10 * 1024 * 1024;

    public int PageSize { get; set; } = 12;

    public List<string> AllowedMimeTypes { get; set; } =
    [
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp",
        "image/bmp"
    ];
}
