using UmbracoCms.Web.Helpers.Aliases;
using UmbracoCms.Web.Helpers.Extensions;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Web.Common.PublishedModels;

namespace UmbracoCms.Web.Components.Cookies;

public class CookieBar : ViewComponentExtended
{
    public required string Text { get; set; } = "";
    public Button? ButtonCookiePage { get; set; }
    public Button? ButtonAgree { get; set; }
    public required string Version { get; set; }

    public IViewComponentResult Invoke(SiteSettings? siteSettings)
    {
        if (siteSettings?.CookiePage is not { } cookiePageLink) return Content("");

        Version = siteSettings.CookieVersion.FallBack("1.0");
        ButtonCookiePage = new Button
        {
            Label = cookiePageLink.Name ?? "",
            Url = cookiePageLink.Url,
            Class = "cookie-bar__button",
            Target = cookiePageLink.Target,
            Hook = "js-hook-cookies-settings-button",
        };
        ButtonAgree = new Button
        {
            Label = CultureDictionary.GetTranslation(TranslationAliases.Common.Cookies.Accept),
            Class = "cookie-bar__button",
            Element = "button",
            Type = "button",
            Variant = "primary",
            Attributes = { ["on:click"] = "cookies::dismiss" },
        };
        Text = siteSettings.CookieText ?? "";
        return View("~/Components/Cookies/CookieBar.cshtml", this);
    }
}
