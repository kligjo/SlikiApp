using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace UmbracoCms.Web.Infrastructure.Middlewares.CustomResponseCaching;

public class CustomResponseCachingMemoryCacheFactory
{
    public CustomResponseCachingMemoryCacheFactory(IOptions<ResponseCachingOptions> options)
    {
        Cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = options.Value.SizeLimit,
        });
    }

    public IMemoryCache Cache { get; }
}

public class ResponseCachingOptions
{
    public long? SizeLimit { get; set; } = 100 * 1024 * 1024; // 100MB default
}
