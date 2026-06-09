using Microsoft.AspNetCore.Razor.TagHelpers;

namespace UmbracoCms.Web.Helpers.TagHelpers;

[HtmlTargetElement("*", Attributes = IfValueAttributeName)]
public class IfAttributeTagHelper : TagHelper
{
    private const string IfValueAttributeName = "asp-if";

    [HtmlAttributeName(IfValueAttributeName)]
    public object? Value { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        bool suppressOutput = Value switch
        {
            bool boolValue => !boolValue,
            string stringValue => string.IsNullOrWhiteSpace(stringValue),
            _ => Value == null,
        };

        if (suppressOutput)
        {
            output.SuppressOutput();
        }
    }
}
