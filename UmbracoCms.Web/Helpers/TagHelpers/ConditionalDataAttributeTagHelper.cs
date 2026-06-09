using Microsoft.AspNetCore.Razor.TagHelpers;

namespace UmbracoCms.Web.Helpers.TagHelpers;

[HtmlTargetElement("*", Attributes = $"{ConditionalDataAttributePrefix}-*")]
public class ConditionalDataAttributeTagHelper : TagHelper
{
    private const string ConditionalDataAttributePrefix = "asp-data";

    [HtmlAttributeName(ConditionalDataAttributePrefix)]
    public IDictionary<string, object?> Values { get; set; } = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        foreach ((string key, object? value) in Values)
        {
            if (value == null) continue;

            int originalIndex = context.AllAttributes.IndexOfName($"{ConditionalDataAttributePrefix}-{key}");
            if (originalIndex == -1) continue;

            int outputIndex = output.Attributes.Count;
            foreach (TagHelperAttribute nextAttribute in context.AllAttributes.Skip(originalIndex + 1))
            {
                int nextAttributeOutputIndex = output.Attributes.IndexOf(nextAttribute);
                if (nextAttributeOutputIndex != -1)
                {
                    outputIndex = nextAttributeOutputIndex;
                    break;
                }
            }

            TagHelperAttribute originalAttribute = context.AllAttributes[originalIndex];
            output.Attributes.Insert(outputIndex, new TagHelperAttribute($"data-{key}", originalAttribute.Value, originalAttribute.ValueStyle));
        }
    }
}
