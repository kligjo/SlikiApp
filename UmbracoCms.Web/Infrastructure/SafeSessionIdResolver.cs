using Umbraco.Cms.Core.Net;

namespace UmbracoCms.Web.Infrastructure;

/// <summary>
/// A re-entrant-safe <see cref="ISessionIdResolver"/> that prevents the infinite recursion caused
/// by Umbraco's default <c>AspNetCoreSessionManager</c>.
///
/// Root cause: <c>HttpSessionIdEnricher</c> fires on every Serilog log event and calls
/// <c>ISessionIdResolver.SessionId</c> → <c>DistributedSession.get_Id()</c> → <c>Load()</c>.
/// <c>Load()</c> itself emits a log event before setting its internal <c>_loaded</c> flag,
/// which triggers the enricher again → stack overflow.
///
/// This resolver uses a <c>[ThreadStatic]</c> re-entrancy flag so that recursive calls return
/// <c>null</c> immediately instead of triggering another session load.
/// </summary>
public sealed class SafeSessionIdResolver : ISessionIdResolver
{
    [ThreadStatic]
    private static bool _isResolving;

    private readonly IHttpContextAccessor _httpContextAccessor;

    public SafeSessionIdResolver(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? SessionId
    {
        get
        {
            if (_isResolving)
            {
                return null;
            }

            _isResolving = true;
            try
            {
                return _httpContextAccessor.HttpContext?.Session?.Id;
            }
            catch
            {
                return null;
            }
            finally
            {
                _isResolving = false;
            }
        }
    }
}
