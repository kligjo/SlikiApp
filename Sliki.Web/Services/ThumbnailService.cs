using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using Microsoft.Extensions.Options;
using Sliki.Web.Options;

namespace Sliki.Web.Services;

public sealed class ThumbnailService
{
    private const int ThumbWidth = 400;

    private readonly string _thumbDir;
    private readonly ILogger<ThumbnailService> _logger;

    public ThumbnailService(
        IOptions<BlobStorageOptions> options,
        IWebHostEnvironment env,
        ILogger<ThumbnailService> logger)
    {
        _logger = logger;

        if (!string.IsNullOrWhiteSpace(options.Value.ThumbnailDirectory))
        {
            _thumbDir = options.Value.ThumbnailDirectory;
        }
        else
        {
            var isAzure = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID"));
            _thumbDir = isAzure
                ? "/home/site/thumbnails"
                : Path.Combine(env.ContentRootPath, "thumbnails");
        }

        Directory.CreateDirectory(_thumbDir);
    }

    public string ThumbPath(string blobName, string subfolder = "") =>
        string.IsNullOrEmpty(subfolder)
            ? Path.Combine(_thumbDir, blobName + ".jpg")
            : Path.Combine(_thumbDir, subfolder, blobName + ".jpg");

    public bool Exists(string blobName, string subfolder = "") =>
        File.Exists(ThumbPath(blobName, subfolder));

    public async Task<bool> GenerateAsync(Stream source, string blobName, string subfolder = "", CancellationToken cancellationToken = default)
    {
        try
        {
            if (!string.IsNullOrEmpty(subfolder))
                Directory.CreateDirectory(Path.Combine(_thumbDir, subfolder));

            source.Position = 0;
            using var image = await Image.LoadAsync(source, cancellationToken);

            if (image.Width > ThumbWidth)
            {
                var height = (int)Math.Round((double)image.Height / image.Width * ThumbWidth);
                image.Mutate(x => x.Resize(ThumbWidth, height));
            }

            var thumbPath = ThumbPath(blobName, subfolder);
            await image.SaveAsJpegAsync(thumbPath, new JpegEncoder { Quality = 72 }, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Thumbnail generation failed for {BlobName}", blobName);
            return false;
        }
    }
}
