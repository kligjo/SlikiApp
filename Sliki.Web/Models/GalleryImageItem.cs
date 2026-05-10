namespace Sliki.Web.Models;

public sealed record GalleryImageItem(
    string BlobName,
    string FileName,
    string ContentType,
    long SizeInBytes,
    DateTimeOffset UploadedAt);
