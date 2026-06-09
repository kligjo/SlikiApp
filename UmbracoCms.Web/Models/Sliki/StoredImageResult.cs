namespace UmbracoCms.Web.Models.Sliki;

public sealed record StoredImageResult(
    string BlobName,
    string FileName,
    string ContentType,
    long SizeInBytes,
    DateTimeOffset UploadedAt);
