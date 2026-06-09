using UmbracoCms.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Web.Common.PublishedModels;

namespace UmbracoCms.Web.Components.Cookies;

public class CookieWall : ViewComponentExtended
{
    public string? Text { get; set; }
    public required Button Button { get; set; }

    public IViewComponentResult Invoke()
    {
        SiteSettings? settings = NodeProvider.SiteSettings;
        if (settings?.CookiePage is not { } cookiePageLink) return Content("");

        Button = new Button
        {
            Label = cookiePageLink.Name ?? "",
            Url = cookiePageLink.Url,
            Target = cookiePageLink.Target,
            Class = "cookie-bar__button",
        };
        Text = settings.CookieMediaWallText?.ToHtmlString();
        return View("~/Components/Cookies/CookieWall.cshtml", this);
    }
}
