using Umbraco.Cms.Core.Models.PublishedContent;

namespace UmbracoCms.Web.Components.NestedBlock;

public class NestedBlockImage : NestedBlock
{
    public required Image Image { get; set; }

    protected override object? ProcessBlock(IPublishedElement block)
    {
        if (block is not Umbraco.Cms.Web.Common.PublishedModels.NestedBlockImage nestedBlockImage) return null;
        Image? image = Image.Create(nestedBlockImage.Image, 1280);
        if (image == null || string.IsNullOrEmpty(image.Url)) return null;
        Image = image;
        Image.Caption = nestedBlockImage.Caption;
        return this;
    }
}
