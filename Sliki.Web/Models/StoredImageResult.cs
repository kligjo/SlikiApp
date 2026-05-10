namespace Sliki.Web.Models;

public sealed record StoredImageResult(
    string BlobName,
    string FileName,
    string ContentType,
    long SizeInBytes,
    DateTimeOffset UploadedAt);
