namespace UmbracoCms.Web.Infrastructure;

public interface ICacheManager
{
    DateTimeOffset LastCacheFlush { get; }
    bool ShouldRequestBeCached(HttpContext context);
    void Flush();
}
