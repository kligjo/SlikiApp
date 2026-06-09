using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;
using UmbracoCms.Web.Infrastructure.Configuration.Options;
using UmbracoCms.Web.Services;

namespace UmbracoCms.Web.Components.NestedBlock;

public sealed class NestedBlockSlikiGallery : NestedBlock
{
    private readonly ImageFileValidator _imageFileValidator;
    private readonly RequestAccessTokenService _requestAccessTokenService;
    private readonly IOptions<AccessTokenOptions> _accessTokenOptions;

    public NestedBlockSlikiGallery(
        ImageFileValidator imageFileValidator,
        RequestAccessTokenService requestAccessTokenService,
        IOptions<AccessTokenOptions> accessTokenOptions)
    {
        _imageFileValidator = imageFileValidator;
        _requestAccessTokenService = requestAccessTokenService;
        _accessTokenOptions = accessTokenOptions;
    }

    protected override object ProcessBlock(IPublishedElement block)
    {
        var title = block.Value<string>("title");
        var introduction = block.Value<string>("introduction");

        return new SlikiGalleryBlockViewModel
        {
            RootId = $"sliki-gallery-{Guid.NewGuid():N}",
            Title = string.IsNullOrWhiteSpace(title) ? "Image gallery" : title,
            Introduction = string.IsNullOrWhiteSpace(introduction)
                ? "Browse everything currently stored in the private sliki container."
                : introduction,
            ListUrl = _requestAccessTokenService.AppendCurrentToken("/api/images"),
            ImageUrlTemplate = _requestAccessTokenService.AppendCurrentToken("/images/__BLOB__"),
            PageSize = _imageFileValidator.PageSize,
            QueryParameterName = _accessTokenOptions.Value.QueryParameterName
        };
    }
}

public sealed class SlikiGalleryBlockViewModel
{
    public required string RootId { get; init; }
    public required string Title { get; init; }
    public required string Introduction { get; init; }
    public required string ListUrl { get; init; }
    public required string ImageUrlTemplate { get; init; }
    public required int PageSize { get; init; }
    public required string QueryParameterName { get; init; }
}
