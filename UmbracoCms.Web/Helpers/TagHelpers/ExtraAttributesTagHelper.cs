using UmbracoCms.Web.Helpers.Extensions;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace UmbracoCms.Web.Helpers.TagHelpers;

[HtmlTargetElement("*", Attributes = ExtraAttributesAttributeName)]
public class ExtraAttributesTagHelper : TagHelper
{
    private const string ExtraAttributesAttributeName = "asp-extra-attribs";

    [HtmlAttributeName(ExtraAttributesAttributeName)]
    public IDictionary<string, string?> ExtraAttributes { get; set; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        foreach ((string name, string? value) in ExtraAttributes)
        {
            output.Attributes.SetAttribute(HtmlHelperExtensions.CreateAttribute(name, value));
        }
    }
}
