using UmbracoCms.Web.Helpers.Extensions;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Web.Common.PublishedModels;

namespace UmbracoCms.Web.Components;

public class Title : ViewComponentExtended
{
    public required string PageTitle { get; set; }

    public IViewComponentResult Invoke(ICompositionBasePage basePage)
    {
        if (basePage is IPublishedContent content)
        {
            PageTitle = content.GetTitle();
        }
        else
        {
            PageTitle = basePage.Title ?? "";
        }
        if (PageTitle is "") return Content("");
        return View("Title", this);
    }
}
