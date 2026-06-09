using UmbracoCms.Web.Infrastructure.Filters;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace UmbracoCms.Web.Controllers;

/// <summary>
/// Represents the default front-end rendering controller.
/// </summary>
/// <remarks>Can be overridden using controller hijacking if required.</remarks>
[CustomResponseCache(Duration = 5 * 60, ServerDuration = 10 * 60)]
public sealed class DefaultRenderController : RenderController
{
    public DefaultRenderController(
        ILogger<RenderController> logger,
        ICompositeViewEngine compositeViewEngine,
        IUmbracoContextAccessor umbracoContextAccessor)
        : base(logger, compositeViewEngine, umbracoContextAccessor)
    {
    }
}
