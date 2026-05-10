namespace Sliki.Web.Models;

public sealed record ImageQuery(
    string? SearchTerm,
    ImageSortBy SortBy,
    int PageNumber,
    int PageSize);
