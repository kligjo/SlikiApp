using Sliki.Web.Models;

namespace Sliki.Web.Services;

public interface IImageStorageService
{
    Task<StoredImageResult> UploadAsync(
        UploadImageRequest request,
        IProgress<long>? progress,
        CancellationToken cancellationToken);

    Task<ImagePageResult> GetImagesAsync(ImageQuery query, CancellationToken cancellationToken);

    Task<BlobImageDownload?> OpenReadAsync(string blobName, CancellationToken cancellationToken);
}
