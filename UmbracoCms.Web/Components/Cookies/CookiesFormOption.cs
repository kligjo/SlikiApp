namespace UmbracoCms.Web.Components.Cookies;

public class CookiesFormOption
{
    public required string Id { get; set; }
    public required string Value { get; set; }
    public required string Label { get; set; }
    public required bool Required { get; set; }

    public static CookiesFormOption? Create(Umbraco.Cms.Web.Common.PublishedModels.NestedBlockCookieOption? option)
    {
        if (option is null || option.CookieId is null or "" || option.Value is null or "" || option.Label is null or "")
            return null;

        return new CookiesFormOption { Id = option.CookieId, Value = option.Value, Label = option.Label, Required = option.Required };
    }
}
