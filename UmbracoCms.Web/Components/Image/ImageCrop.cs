using UmbracoCms.Web.Helpers.Extensions;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors.ValueConverters;

namespace UmbracoCms.Web.Components;

public partial class Image
{
    public class ImageCrop
    {
        public required string Name { get; set; }
        public required ImageCropDimensions Dimensions { get; set; }
        public required string Url { get; set; }

        public static ImageCrop? Create(MediaWithCrops image, ImageCropperValue.ImageCropperCrop c)
        {
            if (c.Alias == null) return null;
            return new ImageCrop
            {
                Name = c.Alias,
                Dimensions = new ImageCropDimensions { Width = c.Width, Height = c.Height },
                Url = image.GetDefaultCropUrl(c.Alias),
            };
        }
    }

    public class ImageCropDimensions
    {
        public required int Width { get; set; }
        public required int Height { get; set; }
    }
}
