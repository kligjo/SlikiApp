using Umbraco.Cms.Core.Dictionary;
using Umbraco.Cms.Web.Common;

namespace UmbracoCms.Web.Helpers.Extensions;

public static class UmbracoTranslationsExtensions
{
    public static string GetTranslation(this UmbracoHelper umbHelper, string key)
    {
        return umbHelper.GetDictionaryValueOrDefault(key, "{{" + key + "}}");
    }

    public static string GetTranslation(this ICultureDictionary cultureDictionary, string key)
    {
        string? dictionaryValue = cultureDictionary[key];
        return !string.IsNullOrWhiteSpace(dictionaryValue) ? dictionaryValue : "{{" + key + "}}";
    }
}
