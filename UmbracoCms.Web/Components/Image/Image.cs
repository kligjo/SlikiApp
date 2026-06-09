using UmbracoCms.Web.Helpers.Aliases;
using UmbracoCms.Web.Helpers.Extensions;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace UmbracoCms.Web.Components;

public partial class Image
{
    public string? Classes { get; set; }
    public string? Preload { get; set; }
    public required string Url { get; set; }
    public string? SrcSet { get; set; }
    public string? Alt { get; set; }
    public string? Caption { get; set; }
    public bool Hidden { get; set; }
    public required bool ObjectFit { get; set; }
    public Dictionary<string, string?> Attributes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string? BackgroundColor { get; set; }
    public bool ShowPreload { get; set; } = true;
    public required List<(ImageCrop ImageCrop, CssBreakpoint Breakpoint)> Crops { get; set; }
    public required (int Width, int Height) AspectRatio { get; set; }

    public static Image? Create(
        MediaWithCrops? mediaWithCrops,
        int width = 0, int height = 0,
        string cssClasses = "", bool objectFit = true,
        IEnumerable<SrcSetEntry>? customSrcSet = null)
    {
        if (mediaWithCrops?.Content == null) return null;
        return Create(mediaWithCrops.Content, width, height, cssClasses, objectFit, customSrcSet,
            mediaWithCrops.LocalCrops.Crops?.Select(c => ImageCrop.Create(mediaWithCrops, c)).WhereNotNull());
    }

    public static Image? Create(
        IPublishedContent? imageContent,
        int width = 0, int height = 0,
        string cssClasses = "", bool objectFit = true,
        IEnumerable<SrcSetEntry>? customSrcSet = null,
        IEnumerable<ImageCrop>? localCrops = null)
    {
        return imageContent switch
        {
            Umbraco.Cms.Web.Common.PublishedModels.Image image => Create(image, width, height, cssClasses, objectFit, customSrcSet, localCrops),
            _ => null,
        };
    }

    private static Image? Create(
        Umbraco.Cms.Web.Common.PublishedModels.Image image,
        int width = 0, int height = 0,
        string cssClasses = "", bool objectFit = true,
        IEnumerable<SrcSetEntry>? customSrcSet = null,
        IEnumerable<ImageCrop>? localCrops = null)
    {
        string? url = image.GetDefaultCropUrl(width, height);
        if (string.IsNullOrEmpty(url)) return null;

        try
        {
            Image? img = new()
            {
                Url = url,
                //TO DO: Decide if we want to fallback to the image name or just have an empty alt attribute if Alt is not set. For SEO it's generally better to have an alt attribute, but it should ideally be descriptive of the image content, and the image name might not always be that.
                //Alt = image.ValueOrDefault(i => i.Alt, image.Name),
                SrcSet = customSrcSet switch { { } srcSet when srcSet.Any() => image.BuildSrcSetString(customSrcSet), _ => default },
                Classes = cssClasses,
                ObjectFit = objectFit,
                Crops = GenerateCrops(localCrops),
                AspectRatio = width != default && height != default ? (width, height) : (16, 9),
            };
            img.AspectRatio = GetAspectRatio(img.Crops);
            return img;
        }
        catch { }
        return null;
    }

    private static List<(ImageCrop ImageCrop, CssBreakpoint Breakpoint)> GenerateCrops(IEnumerable<ImageCrop>? localCrops)
    {
        List<(ImageCrop ImageCrop, CssBreakpoint Breakpoint)> crops = localCrops?
            .Select(imageCrop => (imageCrop, breakpoint: CssBreakpoints.GetBreakpoint(imageCrop.Name)))
            .Where(c => c.breakpoint != null)
            .OrderBy(c => c.breakpoint!.Start ?? 0)
            .Select(c => (c.imageCrop, c.breakpoint!))
            .ToList() ?? [];
        return [.. crops];
    }

    private static (int Width, int Height) GetAspectRatio(IReadOnlyCollection<(ImageCrop ImageCrop, CssBreakpoint Breakpoint)> crops, int width = 0, int height = 0)
    {
        if (crops.Count > 0)
        {
            (ImageCrop imageCrop, _) = crops.Aggregate((curMax, x) => curMax == default || (x.Breakpoint.Priority ?? 0) > (curMax.Breakpoint.Priority ?? 0) ? x : curMax);
            if (imageCrop.Dimensions.Width > 0 && imageCrop.Dimensions.Height > 0)
                return (imageCrop.Dimensions.Width, imageCrop.Dimensions.Height);
        }
        if (width != default && height != default) return (width, height);
        return (16, 9);
    }
}
