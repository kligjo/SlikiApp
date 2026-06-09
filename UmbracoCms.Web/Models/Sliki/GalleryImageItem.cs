namespace UmbracoCms.Web.Models.Sliki;

public sealed record GalleryImageItem(
    string BlobName,
    string FileName,
    string ContentType,
    long SizeInBytes,
    DateTimeOffset UploadedAt);
