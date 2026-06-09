using System.Text.Encodings.Web;
using UmbracoCms.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace UmbracoCms.Web.Helpers.TagHelpers;

[HtmlTargetElement("cache")]
public class CustomCacheTagHelper : CacheTagHelper
{
    private readonly ICacheManager _cacheManager;

    public CustomCacheTagHelper(ICacheManager cacheManager, CacheTagHelperMemoryCacheFactory factory, HtmlEncoder htmlEncoder)
        : base(factory, htmlEncoder)
    {
        _cacheManager = cacheManager;
    }

    public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        Enabled = Enabled && _cacheManager.ShouldRequestBeCached(ViewContext.HttpContext);
        return base.ProcessAsync(context, output);
    }
}
