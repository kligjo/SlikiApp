using Microsoft.Extensions.Caching.Memory;

namespace UmbracoCms.Web.Helpers.Extensions;

public static class MemoryCacheExtensions
{
    public static string GetKey<T>(this IMemoryCache _, string key)
    {
        return $"{typeof(T).Name}__{key}";
    }
}
