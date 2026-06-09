using UmbracoCms.Web.Helpers.Extensions;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Web.Common.PublishedModels;

namespace UmbracoCms.Web.Components.Cookies;

public class CookieVersion : ViewComponentExtended
{
    public required string Version { get; set; }

    public IViewComponentResult Invoke(SiteSettings? siteSettings)
    {
        if (siteSettings?.CookiePage == null) return Content("");
        Version = siteSettings.CookieVersion.FallBack("1.0");
        return View("~/Components/Cookies/CookieVersion.cshtml", this);
    }
}
