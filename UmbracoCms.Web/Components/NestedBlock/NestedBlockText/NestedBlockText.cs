using Umbraco.Cms.Core.Models.PublishedContent;

namespace UmbracoCms.Web.Components.NestedBlock;

public class NestedBlockText : NestedBlock
{
    public required Text Text { get; set; }

    protected override object? ProcessBlock(IPublishedElement block)
    {
        if (block is not Umbraco.Cms.Web.Common.PublishedModels.NestedBlockText nestedBlockText) return null;
        Text? text = Text.Create(nestedBlockText);
        if (text is null || text.Content is "") return null;
        Text = text;
        return this;
    }
}
