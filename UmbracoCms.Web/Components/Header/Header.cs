using UmbracoCms.Web.Helpers.Aliases;
using UmbracoCms.Web.Helpers.Extensions;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Web.Common.PublishedModels;

namespace UmbracoCms.Web.Components;

public class Header : ViewComponentExtended
{
    public required string LogoPath { get; set; }
    public string? LogoUrl { get; set; }
    public required List<Link> Links { get; set; }

    public IViewComponentResult Invoke(PageHome? homePage)
    {
        List<Link> links = homePage?.Children
            .Where(c => !c.IsFolder() && c.IsVisible())
            .Select(c => Link.Create(c, cssClasses: "mega-menu__link"))
            .WhereNotNull()
            .ToList() ?? [];

        LogoPath = SvgAliases.Logo;
        Links = links;
        LogoUrl = homePage?.Url();
        return View("Header", this);
    }
}
