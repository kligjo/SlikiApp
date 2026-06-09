using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace UmbracoCms.Web.Controllers;

public abstract class AsyncRenderController : RenderController
{
    protected AsyncRenderController(
        ILogger<RenderController> logger,
        ICompositeViewEngine compositeViewEngine,
        IUmbracoContextAccessor umbracoContextAccessor)
        : base(logger, compositeViewEngine, umbracoContextAccessor)
    {
    }

    [NonAction]
    public sealed override IActionResult Index()
    {
        throw new NotImplementedException();
    }

    public abstract Task<IActionResult> Index(CancellationToken cancellationToken);
}
