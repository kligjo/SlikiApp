using UmbracoCms.Web.Helpers.Extensions;

namespace UmbracoCms.Web.Components;

public partial class Image
{
    public class SrcSetEntry
    {
        public int Breakpoint { get; private init; }
        public int? Width { get; private set; }
        public int? Height { get; private set; }
        public double? HeightRatio { get; private set; }

        public static SrcSetEntry At(int breakpoint) => new() { Breakpoint = breakpoint };
        public SrcSetEntry WithWidth(int? width) { Width = width; return this; }
        public SrcSetEntry WithHeight(int? height) { Height = height; return this; }
        public SrcSetEntry WithHeightRatio(double? heightRatio) { HeightRatio = heightRatio; return this; }

        public string ToString(Umbraco.Cms.Web.Common.PublishedModels.Image image)
        {
            int imageWidth = Width ?? Breakpoint;
            string imageUrl = $"{image.GetDefaultCropUrl(imageWidth, Height)}";
            if (HeightRatio != null) imageUrl += $"&heightratio={HeightRatio}".Replace(",", ".");
            return $"{imageUrl} {Breakpoint}w";
        }
    }
}
