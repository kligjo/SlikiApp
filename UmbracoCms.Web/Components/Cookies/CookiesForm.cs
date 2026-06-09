using Microsoft.AspNetCore.Mvc;

namespace UmbracoCms.Web.Components.Cookies;

public class CookiesForm : ViewComponentExtended
{
    public required string PageUrl { get; set; }
    public string? Text { get; set; }
    public required List<CookiesFormOption> CookieOptions { get; set; }
    public string? CssClass { get; set; }

    public IViewComponentResult Invoke(Umbraco.Cms.Web.Common.PublishedModels.PageCookies page)
    {
        List<CookiesFormOption>? options = page.CookiesOptions?
            .Select(o => o.Content as Umbraco.Cms.Web.Common.PublishedModels.NestedBlockCookieOption)
            .Select(CookiesFormOption.Create)
            .WhereNotNull()
            .ToList();

        if (options is not { Count: > 0 }) return Content("");

        PageUrl = page.Url();
        CookieOptions = options;
        Text = page.CookiesText?.ToString();
        return View("~/Components/Cookies/CookiesForm.cshtml", this);
    }
}
