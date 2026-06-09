using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using UmbracoCms.Web.Infrastructure.Configuration.Options;
using UmbracoCms.Web.Infrastructure.Middlewares.CustomResponseCaching;
using Umbraco.Cms.Core.Web;

namespace UmbracoCms.Web.Infrastructure;

public class CacheManager : ICacheManager, IDisposable
{
    private readonly CustomResponseCachingMemoryCacheFactory _responseCachingFactory;
    private readonly CacheTagHelperMemoryCacheFactory _cacheTagHelperFactory;
    private readonly IOptionsMonitor<CacheOptions> _cacheOptions;
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;
    private readonly IDisposable? _changeTracker;

    public CacheManager(
        CustomResponseCachingMemoryCacheFactory responseCachingFactory,
        CacheTagHelperMemoryCacheFactory cacheTagHelperFactory,
        IOptionsMonitor<CacheOptions> cacheOptions,
        IUmbracoContextAccessor umbracoContextAccessor)
    {
        _responseCachingFactory = responseCachingFactory;
        _cacheTagHelperFactory = cacheTagHelperFactory;
        _cacheOptions = cacheOptions;
        _umbracoContextAccessor = umbracoContextAccessor;
        _changeTracker = _cacheOptions.OnChange(_ => Flush());
    }

    public DateTimeOffset LastCacheFlush { get; private set; } = DateTimeOffset.UtcNow;

    public bool ShouldRequestBeCached(HttpContext context)
    {
        if (!_cacheOptions.CurrentValue.Enabled)
        {
            return false;
        }

        if (context.Request.Headers.CacheControl.Any(h => h?.Contains("no-cache") == true))
        {
            return false;
        }

        string path = context.Request.Path.Value ?? "";
        if (path.StartsWith("/umbraco", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (_umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext) && umbracoContext.InPreviewMode)
        {
            return false;
        }

        return true;
    }

    public void Flush()
    {
        if (_responseCachingFactory.Cache is MemoryCache responseCache) responseCache.Compact(100);
        if (_cacheTagHelperFactory.Cache is MemoryCache tagCache) tagCache.Compact(100);
        LastCacheFlush = DateTimeOffset.UtcNow;
    }

    public void Dispose()
    {
        _changeTracker?.Dispose();
        Flush();
    }
}
