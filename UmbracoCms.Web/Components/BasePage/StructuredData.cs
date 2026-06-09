using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Web.Common.PublishedModels;

namespace UmbracoCms.Web.Components.BasePage;

public class StructuredData : ViewComponentExtended
{
    public string? CompanyName { get; set; }
    public string? HomePageUrl { get; set; }
    public string? CompanyLogo { get; set; }

    public IViewComponentResult Invoke(PageHome? homePage, SiteSettings? siteSettings)
    {
        CompanyName = siteSettings?.CompanyName;
        CompanyLogo = siteSettings?.CompanyLogo?.GetCropUrl(1200, 630);
        HomePageUrl = homePage?.Url();
        return View("~/Components/BasePage/StructuredData.cshtml", this);
    }
}
