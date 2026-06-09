using UmbracoCms.Web.Helpers.Extensions;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace UmbracoCms.Web.Components;

public class Breadcrumb : ViewComponentExtended
{
    public required List<Link> Pages { get; set; }

    public IViewComponentResult Invoke(IPublishedContent page)
    {
        Pages = page.AncestorsOrSelf()
            .Where(ancestor => !ancestor.IsFolder())
            .OrderBy(x => x.Level)
            .Select(l => Link.Create(l, false))
            .WhereNotNull()
            .ToList();

        return View("Breadcrumb", this);
    }
}
