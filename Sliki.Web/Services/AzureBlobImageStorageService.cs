using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;
using Sliki.Web.Models;
using Sliki.Web.Options;
using Sliki.Web.Utilities;

namespace Sliki.Web.Services;

public class AzureBlobImageStorageService : IImageStorageService
{
    private const string OriginalFileNameMetadataKey = "originalfilename";

    private readonly BlobContainerClient _containerClient;
    private readonly BlobStorageOptions _options;
    private readonly SemaphoreSlim _containerInitLock = new(1, 1);
    private bool _containerExists;
    private List<GalleryImageItem>? _cache;
    private DateTimeOffset _cacheExpiry;

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

        _cache = null;

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

        List<GalleryImageItem> images;

        if (_cache is not null && DateTimeOffset.UtcNow < _cacheExpiry)
        {
            images = _cache;
        }
        else
        {
            images = [];

            await foreach (var blobItem in _containerClient.GetBlobsAsync(
                new GetBlobsOptions { Traits = BlobTraits.Metadata },
                cancellationToken: cancellationToken))
            {
                var fileName = blobItem.Metadata.TryGetValue(OriginalFileNameMetadataKey, out var originalFileName)
                    ? originalFileName
                    : blobItem.Name;

                images.Add(new GalleryImageItem(
                    blobItem.Name,
                    fileName,
                    blobItem.Properties.ContentType ?? "application/octet-stream",
                    blobItem.Properties.ContentLength ?? 0,
                    blobItem.Properties.LastModified ?? DateTimeOffset.MinValue));
            }

            _cache = images;
            _cacheExpiry = DateTimeOffset.UtcNow.AddMinutes(2);
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

        return new ImagePageResult(images, totalCount, 1, totalCount);
    }

    public async Task<BlobImageDownload?> OpenReadAsync(string blobName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(blobName))
        {
            return null;
        }

        try
        {
                await EnsureContainerExistsAsync(cancellationToken);

            var blobClient = _containerClient.GetBlobClient(blobName);

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
        catch (OperationCanceledException)
        {
            // Client canceled the request (e.g., navigated away, closed browser, or timeout)
            // This is expected behavior, especially for large video files
            return null;
        }
    }

    public async Task SetBlobOriginalFileNameAsync(string blobName, string fileName, CancellationToken cancellationToken)
    {
        var blobClient = _containerClient.GetBlobClient(blobName);
        await blobClient.SetMetadataAsync(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [OriginalFileNameMetadataKey] = fileName
            },
            cancellationToken: cancellationToken);
        _cache = null;
    }

    public async Task<SasUploadTicket> GenerateSasUploadUrlAsync(
        string fileName,
        string contentType,
        CancellationToken cancellationToken)
    {
        await EnsureContainerExistsAsync(cancellationToken);

        var safeStem = FileNameSanitizer.SanitizeStem(Path.GetFileNameWithoutExtension(fileName));
        var extension = FileNameSanitizer.GetExtensionForContentType(contentType)
            ?? Path.GetExtension(fileName).ToLowerInvariant();
        var safeDisplayName = FileNameSanitizer.SanitizeDisplayName($"{safeStem}{extension}");
        var blobName = $"{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}-{safeStem}{extension}";

        var blobClient = _containerClient.GetBlobClient(blobName);
        var expiry = DateTimeOffset.UtcNow.AddHours(2);

        Uri sasUri;

        if (_containerClient.CanGenerateSasUri)
        {
            // Connection string path — account key available
            var sasBuilder = new BlobSasBuilder(BlobSasPermissions.Write | BlobSasPermissions.Create, expiry)
            {
                BlobContainerName = _containerClient.Name,
                BlobName = blobName,
                Resource = "b",
                ContentType = contentType
            };
            sasUri = blobClient.GenerateSasUri(sasBuilder);
        }
        else
        {
            // Managed Identity path — use User Delegation SAS
            // Requires "Storage Blob Delegator" role on the storage account
            var delegationKey = await _containerClient.GetParentBlobServiceClient()
                .GetUserDelegationKeyAsync(DateTimeOffset.UtcNow.AddMinutes(-5), expiry, cancellationToken);

            var sasBuilder = new BlobSasBuilder(BlobSasPermissions.Write | BlobSasPermissions.Create, expiry)
            {
                BlobContainerName = _containerClient.Name,
                BlobName = blobName,
                Resource = "b",
                ContentType = contentType
            };
            var accountName = _containerClient.AccountName;
            var sasParams = sasBuilder.ToSasQueryParameters(delegationKey, accountName);
            var uriBuilder = new BlobUriBuilder(blobClient.Uri) { Sas = sasParams };
            sasUri = uriBuilder.ToUri();
        }

        _cache = null;

        return new SasUploadTicket(blobName, safeDisplayName, sasUri.ToString(), contentType);
    }

    private async Task EnsureContainerExistsAsync(CancellationToken cancellationToken)
    {
        if (_containerExists)
        {
            return;
        }

        await _containerInitLock.WaitAsync(cancellationToken);
        try
        {
            if (_containerExists)
            {
                return;
            }

            await _containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
            _containerExists = true;
        }
        finally
        {
            _containerInitLock.Release();
        }
    }
}
