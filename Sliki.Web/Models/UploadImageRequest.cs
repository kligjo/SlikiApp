namespace Sliki.Web.Models;

public sealed record UploadImageRequest(
    string FileName,
    string ContentType,
    long SizeInBytes,
    Stream Content);
