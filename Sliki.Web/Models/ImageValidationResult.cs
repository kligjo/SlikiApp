namespace Sliki.Web.Models;

public sealed record ImageValidationResult(
    bool IsValid,
    string? ErrorMessage,
    string? NormalizedContentType)
{
    public static ImageValidationResult Success(string contentType) => new(true, null, contentType);

    public static ImageValidationResult Failure(string errorMessage) => new(false, errorMessage, null);
}
