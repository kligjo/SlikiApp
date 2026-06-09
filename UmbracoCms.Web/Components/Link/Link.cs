using UmbracoCms.Web.Helpers.Extensions;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace UmbracoCms.Web.Components;

public class Link
{
    public string? Label { get; set; }
    public required string Url { get; set; }
    public string? Target { get; set; }
    public Dictionary<string, string?> Attributes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string? CssClasses { get; set; }
    public string? IconPath { get; set; }

    public static Link? Create(IPublishedContent? node, bool showTitle = true, string? cssClasses = null)
    {
        if (node == null) return null;
        string? label = node.Name;
        if (showTitle && node.GetTitle() is { Length: > 0 } title) label = title;
        return new Link { Url = node.Url(), Label = label, CssClasses = cssClasses };
    }

    public static Link? Create(Umbraco.Cms.Core.Models.Link? link, string? cssClasses = null, string? icon = null, bool hideLabel = false)
    {
        if (link == null) return null;
        return new Link { Url = link.Url ?? "", Target = link.Target, Label = !hideLabel ? link.Name : null, CssClasses = cssClasses, IconPath = icon };
    }
}
