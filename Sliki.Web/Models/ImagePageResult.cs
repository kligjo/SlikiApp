namespace Sliki.Web.Models;

public sealed record ImagePageResult(
    IReadOnlyList<GalleryImageItem> Items,
    int TotalCount,
    int PageNumber,
    int PageSize)
{
    public int TotalPages => TotalCount == 0
        ? 1
        : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
