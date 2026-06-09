using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Web.Common.PublishedModels;

namespace UmbracoCms.Web.Components.NestedBlock;

public abstract class NestedBlock : ViewComponentExtended
{
    public string? Id { get; set; }
    protected virtual string DefaultViewName => GetType().Name;

    public async Task<IViewComponentResult> InvokeAsync(BlockListItem item, string? altView = null)
    {
        if (item.Settings is not null)
        {
            ProcessSettings(item.Settings);
        }
        object? model = await ProcessBlockAsync(item.Content);
        return RenderBlock(model, altView);
    }

    protected virtual void ProcessSettings(IPublishedElement settings)
    {
        if (settings is DefaultComponentSettings defaultSettings) Id = defaultSettings.Identifier;
    }

    protected virtual object? ProcessBlock(IPublishedElement block) =>
        throw new NotImplementedException($"{GetType().FullName} must implement either {nameof(ProcessBlock)} or {nameof(ProcessBlockAsync)}");

    protected virtual Task<object?> ProcessBlockAsync(IPublishedElement block) => Task.FromResult(ProcessBlock(block));

    protected virtual IViewComponentResult RenderBlock(object? model, string? altView)
    {
        if (model == null) return Content("");
        string viewPath = EnsureAltViewExists(altView) ?? $"~/Components/NestedBlock/{DefaultViewName}/{DefaultViewName}.cshtml";
        return View(viewPath, model);
    }

    private string? EnsureAltViewExists(string? altView)
    {
        if (altView is null or "") return null;
        string altViewPath = $"~/Components/NestedBlock/{altView}.cshtml";
        return ViewEngine.GetView(ViewContext.ExecutingFilePath, altViewPath, false).Success ? altViewPath : null;
    }
}
