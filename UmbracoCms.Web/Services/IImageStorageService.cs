using UmbracoCms.Web.Models.Sliki;

namespace UmbracoCms.Web.Services;

public interface IImageStorageService
{
    Task<StoredImageResult> UploadAsync(
        UploadImageRequest request,
        IProgress<long>? progress,
        CancellationToken cancellationToken);

    Task<ImagePageResult> GetImagesAsync(ImageQuery query, CancellationToken cancellationToken);

    Task<BlobImageDownload?> OpenReadAsync(string blobName, CancellationToken cancellationToken);
}
