namespace Sliki.Web.Models;

public sealed record BlobImageDownload(
    Stream Content,
    string FileName,
    string ContentType,
    DateTimeOffset LastModified,
    string ETag);
