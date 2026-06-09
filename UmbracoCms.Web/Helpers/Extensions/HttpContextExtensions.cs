using Umbraco.Cms.Web.Common.Controllers;

namespace UmbracoCms.Web.Helpers.Extensions;

public static class HttpContextExtensions
{
    public static bool IsUmbracoPageRequest(this HttpContext context)
    {
        return context.GetEndpoint()
            ?.Metadata.GetMetadata<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>()
            ?.ControllerTypeInfo.IsAssignableTo(typeof(IRenderController)) == true;
    }
}
