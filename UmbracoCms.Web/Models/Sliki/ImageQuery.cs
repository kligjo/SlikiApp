namespace UmbracoCms.Web.Models.Sliki;

public sealed record ImageQuery(
    string? SearchTerm,
    ImageSortBy SortBy,
    int PageNumber,
    int PageSize);
