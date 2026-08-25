namespace Sliki.Web.Models;

public sealed record SasUploadTicket(
    string BlobName,
    string FileName,
    string SasUrl,
    string ContentType);
