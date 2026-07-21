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

    public string ThumbPath(string blobName) =>
        Path.Combine(_thumbDir, blobName + ".jpg");

    public bool Exists(string blobName) =>
        File.Exists(ThumbPath(blobName));

    public async Task<bool> GenerateAsync(Stream source, string blobName, CancellationToken cancellationToken = default)
    {
        try
        {
            source.Position = 0;
            using var image = await Image.LoadAsync(source, cancellationToken);

            if (image.Width > ThumbWidth)
            {
                var height = (int)Math.Round((double)image.Height / image.Width * ThumbWidth);
                image.Mutate(x => x.Resize(ThumbWidth, height));
            }

            var thumbPath = ThumbPath(blobName);
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
