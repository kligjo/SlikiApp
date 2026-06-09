using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;
using UmbracoCms.Web.Infrastructure.Configuration.Options;
using UmbracoCms.Web.Services;
using UmbracoCms.Web.Utilities;

namespace UmbracoCms.Web.Components.NestedBlock;

public sealed class NestedBlockSlikiUploader : NestedBlock
{
    private readonly IOptions<BlobStorageOptions> _blobStorageOptions;
    private readonly ImageFileValidator _imageFileValidator;
    private readonly RequestAccessTokenService _requestAccessTokenService;

    public NestedBlockSlikiUploader(
        IOptions<BlobStorageOptions> blobStorageOptions,
        ImageFileValidator imageFileValidator,
        RequestAccessTokenService requestAccessTokenService)
    {
        _blobStorageOptions = blobStorageOptions;
        _imageFileValidator = imageFileValidator;
        _requestAccessTokenService = requestAccessTokenService;
    }

    protected override object ProcessBlock(IPublishedElement block)
    {
        var title = block.Value<string>("title");
        var introduction = block.Value<string>("introduction");

        return new SlikiUploaderBlockViewModel
        {
            RootId = $"sliki-uploader-{Guid.NewGuid():N}",
            Title = string.IsNullOrWhiteSpace(title) ? "Upload images" : title,
            Introduction = string.IsNullOrWhiteSpace(introduction)
                ? "Browse for one or more images and upload them to the private sliki container."
                : introduction,
            AcceptedMimeTypes = _imageFileValidator.AcceptAttributeValue,
            MaxUploadBytes = _blobStorageOptions.Value.MaxUploadBytes,
            MaxUploadDisplay = FileSizeFormatter.Format(_blobStorageOptions.Value.MaxUploadBytes),
            UploadUrl = _requestAccessTokenService.AppendCurrentToken("/api/images/upload")
        };
    }
}

public sealed class SlikiUploaderBlockViewModel
{
    public required string RootId { get; init; }
    public required string Title { get; init; }
    public required string Introduction { get; init; }
    public required string AcceptedMimeTypes { get; init; }
    public required long MaxUploadBytes { get; init; }
    public required string MaxUploadDisplay { get; init; }
    public required string UploadUrl { get; init; }
}
