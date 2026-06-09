using Umbraco.Cms.Web.Common.PublishedModels;

namespace UmbracoCms.Web.Components;

public class Text
{
    public string? Title { get; set; }
    public required string Content { get; set; }
    public string? CssClasses { get; set; }

    public static Text? Create(NestedBlockText block, string? css = null)
    {
        string? content = block.Text?.ToHtmlString();
        if (string.IsNullOrEmpty(content)) return null;
        return new Text { Content = content, Title = block.ShowTitle ? block.Title : null, CssClasses = css };
    }
}
