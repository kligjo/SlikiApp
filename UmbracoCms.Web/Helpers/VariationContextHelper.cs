using UmbracoCms.Web.Helpers.Extensions;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace UmbracoCms.Web.Helpers;

public sealed class VariationContextHelper : IDisposable
{
    private readonly IVariationContextAccessor _variationContextAccessor;
    private VariationContext? _originalVariationContext;

    public VariationContextHelper(IVariationContextAccessor variationContextAccessor, string? culture, string? segment = null)
    {
        _variationContextAccessor = variationContextAccessor;
        VariationContext? originalVariationContext = variationContextAccessor.VariationContext;

        if (variationContextAccessor.SetVariationContext(culture, segment))
        {
            _originalVariationContext = originalVariationContext;
        }
    }

    public void Dispose()
    {
        if (_originalVariationContext != null)
        {
            _variationContextAccessor.VariationContext = _originalVariationContext;
        }
        _originalVariationContext = null;
    }
}
