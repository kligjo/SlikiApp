using UmbracoCms.Web.Services.Assets;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace UmbracoCms.Web.Helpers.TagHelpers;

[HtmlTargetElement("svg", Attributes = SourceAttributeName)]
public class SvgTagHelper : TagHelper
{
    private const string SourceAttributeName = "src";
    private readonly IAssetsProvider _assetsProvider;

    public SvgTagHelper(IAssetsProvider assetsProvider)
    {
        _assetsProvider = assetsProvider;
    }

    [HtmlAttributeName(SourceAttributeName)]
    public string? Src { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = string.Empty;
        if (!string.IsNullOrEmpty(Src))
        {
            string? svgContent = await _assetsProvider.GetContent(Src);
            output.Content.SetHtmlContent(svgContent);
        }
    }
}
