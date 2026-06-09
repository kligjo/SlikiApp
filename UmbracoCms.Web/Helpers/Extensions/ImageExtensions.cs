using UmbracoCms.Web.Components;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace UmbracoCms.Web.Helpers.Extensions;

public static class ImageExtensions
{
    public static string? GetDefaultCropUrl(
        this Umbraco.Cms.Web.Common.PublishedModels.Image image,
        int? width = null,
        int? height = null,
        int quality = 80,
        UrlMode urlMode = UrlMode.Default)
    {
        Components.Image.ImageCropDimensions dimensions = GetImageCropDimensions(image, width, height);
        return image.GetCropUrl(dimensions.Width, dimensions.Height, quality: quality, imageCropMode: ImageCropMode.Crop, urlMode: urlMode);
    }

    public static string GetDefaultCropUrl(
        this MediaWithCrops image,
        string cropAlias,
        int quality = 80,
        UrlMode urlMode = UrlMode.Default)
    {
        if (image.LocalCrops.GetCrop(cropAlias) is not { } crop)
        {
            return "";
        }

        Components.Image.ImageCropDimensions dimensions = GetImageCropDimensions(image.Content, crop.Width, crop.Height);
        string? cropUrl = image.GetCropUrl(dimensions.Width, dimensions.Height, cropAlias: cropAlias, quality: quality, imageCropMode: ImageCropMode.Crop, urlMode: urlMode);

        if (cropUrl is not null && crop.Coordinates is not null)
        {
            // Use a simple query string append instead of Flurl
            cropUrl = AppendQueryParam(cropUrl, "rmode", nameof(ImageCropMode.Min).ToLowerInvariant());
        }

        return cropUrl ?? image.MediaUrl();
    }

    public static string BuildSrcSetString(this Umbraco.Cms.Web.Common.PublishedModels.Image image, IEnumerable<Components.Image.SrcSetEntry> entries)
    {
        return string.Join(",", entries.Select(x => x.ToString(image)));
    }

    private static Components.Image.ImageCropDimensions GetImageCropDimensions(IPublishedContent node, int? width, int? height)
    {
        int currentWidth = width ?? 0;
        int currentHeight = height ?? 0;

        if (node is not Umbraco.Cms.Web.Common.PublishedModels.Image img || (img.UmbracoWidth >= currentWidth && img.UmbracoHeight >= currentHeight))
        {
            return new Components.Image.ImageCropDimensions { Width = currentWidth, Height = currentHeight };
        }

        if (currentWidth == 0)
        {
            return new Components.Image.ImageCropDimensions { Width = 0, Height = Math.Min(currentHeight, img.UmbracoHeight) };
        }

        if (currentHeight == 0)
        {
            return new Components.Image.ImageCropDimensions { Width = Math.Min(currentWidth, img.UmbracoWidth), Height = 0 };
        }

        double ratio = currentWidth / (double)currentHeight;
        int maxWidth = Math.Min(img.UmbracoWidth, currentWidth);
        double maxHeight = Math.Min(img.UmbracoHeight, maxWidth / ratio);
        int newWidth = (int)Math.Round(maxHeight * ratio);
        int newHeight = (int)Math.Round(maxHeight);

        return new Components.Image.ImageCropDimensions { Width = newWidth, Height = newHeight };
    }

    private static string AppendQueryParam(string url, string key, string value)
    {
        char separator = url.Contains('?') ? '&' : '?';
        return $"{url}{separator}{key}={value}";
    }
}
