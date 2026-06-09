using UmbracoCms.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Dictionary;

namespace UmbracoCms.Web.Components;

public abstract class ViewComponentExtended : ViewComponent
{
    private NodeProvider? _nodeProvider;
    private ICultureDictionary? _cultureDictionary;

    public NodeProvider NodeProvider => _nodeProvider ??= HttpContext.RequestServices.GetRequiredService<NodeProvider>();
    public ICultureDictionary CultureDictionary => _cultureDictionary ??= HttpContext.RequestServices.GetRequiredService<ICultureDictionary>();
}
