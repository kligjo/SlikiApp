using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using UmbracoCms.Web.Infrastructure.Configuration.Options;
using UmbracoCms.Web.Models.Sliki;
using UmbracoCms.Web.Utilities;

namespace UmbracoCms.Web.Services;

public sealed class AzureBlobImageStorageService : IImageStorageService
{
    private const string OriginalFileNameMetadataKey = "originalfilename";

    private readonly BlobContainerClient _containerClient;
    private readonly BlobStorageOptions _options;

    public AzureBlobImageStorageService(
        BlobServiceClient blobServiceClient,
        IOptions<BlobStorageOptions> options)
    {
        _options = options.Value;
        _containerClient = blobServiceClient.GetBlobContainerClient(_options.ContainerName);
    }

    public async Task<StoredImageResult> UploadAsync(
        UploadImageRequest request,
        IProgress<long>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.FileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ContentType);

        await EnsureContainerExistsAsync(cancellationToken);

        var safeStem = FileNameSanitizer.SanitizeStem(Path.GetFileNameWithoutExtension(request.FileName));
        var extension = FileNameSanitizer.GetExtensionForContentType(request.ContentType)
            ?? Path.GetExtension(request.FileName).ToLowerInvariant();
        var safeDisplayName = FileNameSanitizer.SanitizeDisplayName($"{safeStem}{extension}");
        var blobName = $"{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}-{safeStem}{extension}";

        if (request.Content.CanSeek)
        {
            request.Content.Position = 0;
        }

        var blobClient = _containerClient.GetBlobClient(blobName);
        var now = DateTimeOffset.UtcNow;

        await blobClient.UploadAsync(
            request.Content,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = request.ContentType,
                    ContentDisposition = "inline"
                },
                Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [OriginalFileNameMetadataKey] = safeDisplayName
                },
                Conditions = new BlobRequestConditions
                {
                    IfNoneMatch = ETag.All
                },
                ProgressHandler = progress is null ? null : new Progress<long>(progress.Report)
            },
            cancellationToken);

        return new StoredImageResult(
            blobName,
            safeDisplayName,
            request.ContentType,
            request.SizeInBytes,
            now);
    }

    public async Task<ImagePageResult> GetImagesAsync(ImageQuery query, CancellationToken cancellationToken)
    {
        await EnsureContainerExistsAsync(cancellationToken);

        var images = new List<GalleryImageItem>();

        await foreach (var blobItem in _containerClient.GetBlobsAsync(
                           new GetBlobsOptions
                           {
                               Traits = BlobTraits.Metadata
                           },
                           cancellationToken: cancellationToken))
        {
            var fileName = blobItem.Metadata.TryGetValue(OriginalFileNameMetadataKey, out var originalFileName)
                ? originalFileName
                : blobItem.Name;

            images.Add(
                new GalleryImageItem(
                    blobItem.Name,
                    fileName,
                    blobItem.Properties.ContentType ?? "application/octet-stream",
                    blobItem.Properties.ContentLength ?? 0,
                    blobItem.Properties.LastModified ?? DateTimeOffset.MinValue));
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            images = images
                .Where(image => image.FileName.Contains(query.SearchTerm, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        images = query.SortBy switch
        {
            ImageSortBy.OldestFirst => images.OrderBy(image => image.UploadedAt).ToList(),
            ImageSortBy.NameAscending => images.OrderBy(image => image.FileName, StringComparer.OrdinalIgnoreCase).ToList(),
            ImageSortBy.NameDescending => images.OrderByDescending(image => image.FileName, StringComparer.OrdinalIgnoreCase).ToList(),
            _ => images.OrderByDescending(image => image.UploadedAt).ToList()
        };

        var totalCount = images.Count;
        var pageNumber = Math.Max(1, query.PageNumber);
        var pageSize = Math.Clamp(query.PageSize, 1, Math.Max(1, _options.PageSize));
        var pageItems = images
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new ImagePageResult(pageItems, totalCount, pageNumber, pageSize);
    }

    public async Task<BlobImageDownload?> OpenReadAsync(string blobName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(blobName))
        {
            return null;
        }

        await EnsureContainerExistsAsync(cancellationToken);

        var blobClient = _containerClient.GetBlobClient(blobName);

        try
        {
            var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
            var stream = await blobClient.OpenReadAsync(cancellationToken: cancellationToken);
            var fileName = properties.Value.Metadata.TryGetValue(OriginalFileNameMetadataKey, out var originalFileName)
                ? originalFileName
                : blobName;

            return new BlobImageDownload(
                stream,
                fileName,
                properties.Value.ContentType ?? "application/octet-stream",
                properties.Value.LastModified,
                properties.Value.ETag.ToString());
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    private Task EnsureContainerExistsAsync(CancellationToken cancellationToken) =>
        _containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
}
