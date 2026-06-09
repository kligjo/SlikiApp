namespace UmbracoCms.Web.Models.Sliki;

public sealed record BlobImageDownload(
    Stream Content,
    string FileName,
    string ContentType,
    DateTimeOffset LastModified,
    string ETag);
