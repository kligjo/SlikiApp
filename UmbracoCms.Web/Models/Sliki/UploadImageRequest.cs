namespace UmbracoCms.Web.Models.Sliki;

public sealed record UploadImageRequest(
    string FileName,
    string ContentType,
    long SizeInBytes,
    Stream Content);
