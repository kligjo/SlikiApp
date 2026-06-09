using UmbracoCms.Web.Helpers.Extensions;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Umbraco.Cms.Core.Dictionary;

namespace UmbracoCms.Web.Helpers.TagHelpers;

[HtmlTargetElement("*", Attributes = KeyAttributeName)]
public class TranslationTagHelper : TagHelper
{
    private const string KeyAttributeName = "asp-translation";
    private readonly ICultureDictionary _cultureDictionary;

    public TranslationTagHelper(ICultureDictionary cultureDictionary)
    {
        _cultureDictionary = cultureDictionary;
    }

    [HtmlAttributeName(KeyAttributeName)]
    public string? Key { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (!string.IsNullOrEmpty(Key))
        {
            string translation = _cultureDictionary.GetTranslation(Key);
            output.Content.SetHtmlContent(translation);
        }
        _ = output.Attributes.RemoveAll(KeyAttributeName);
    }
}
