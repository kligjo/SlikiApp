using System.Threading.Channels;

namespace Sliki.Web.Services;

public sealed class ThumbnailQueue : BackgroundService
{
    // Bounded so we don't buffer gigabytes of raw image data in memory
    private readonly Channel<QueueItem> _channel = Channel.CreateBounded<QueueItem>(
        new BoundedChannelOptions(1000) { FullMode = BoundedChannelFullMode.Wait });

    private readonly ThumbnailService _thumbnailService;
    private readonly ILogger<ThumbnailQueue> _logger;

    public ThumbnailQueue(ThumbnailService thumbnailService, ILogger<ThumbnailQueue> logger)
    {
        _thumbnailService = thumbnailService;
        _logger = logger;
    }

    // Called from upload endpoints — returns as soon as the item is queued
    public ValueTask EnqueueAsync(string blobName, string subfolder, byte[] data, CancellationToken ct = default) =>
        _channel.Writer.WriteAsync(new QueueItem(blobName, subfolder, data), ct);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var item in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var ms = new MemoryStream(item.Data, writable: false);
                await _thumbnailService.GenerateAsync(ms, item.BlobName, item.Subfolder, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Queued thumbnail generation failed for {BlobName}", item.BlobName);
            }
        }
    }

    private sealed record QueueItem(string BlobName, string Subfolder, byte[] Data);
}
