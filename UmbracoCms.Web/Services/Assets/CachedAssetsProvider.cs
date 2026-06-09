using Microsoft.Extensions.Caching.Memory;
using UmbracoCms.Web.Helpers.Extensions;

namespace UmbracoCms.Web.Services.Assets;

public class CachedAssetsProvider : IAssetsProvider
{
    private readonly IAssetsProvider _inner;
    private readonly IMemoryCache _cache;

    public CachedAssetsProvider(IAssetsProvider inner, IMemoryCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public async Task<string?> GetContent(string path)
    {
        string cacheKey = _cache.GetKey<CachedAssetsProvider>(path);

        if (_cache.TryGetValue(cacheKey, out string? cachedContent))
        {
            return cachedContent;
        }

        string? content = await _inner.GetContent(path);
        if (content != null)
        {
            _cache.Set(cacheKey, content, new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(60),
            });
        }

        return content;
    }
}
