using Microsoft.Extensions.Options;
using UmbracoCms.Web.Infrastructure.Configuration.Options;
using UmbracoCms.Web.Utilities;

namespace UmbracoCms.Web.Services;

public sealed class RequestAccessTokenService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AccessTokenOptions _options;

    public RequestAccessTokenService(
        IHttpContextAccessor httpContextAccessor,
        IOptions<AccessTokenOptions> options)
    {
        _httpContextAccessor = httpContextAccessor;
        _options = options.Value;
    }

    public string QueryParameterName => _options.QueryParameterName;

    public string? GetCurrentToken()
    {
        var context = _httpContextAccessor.HttpContext;
        return context is not null
            && SharedAccessTokenHelper.TryResolveRequestToken(context, _options, out var token)
            ? token
            : null;
    }

    public string AppendCurrentToken(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || !ShouldAppendTo(url))
        {
            return url ?? string.Empty;
        }

        return AccessTokenUrlHelper.AppendToken(url, _options.QueryParameterName, GetCurrentToken());
    }

    private bool ShouldAppendTo(string url)
    {
        if (url.StartsWith('#')
            || url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)
            || url.StartsWith("tel:", StringComparison.OrdinalIgnoreCase)
            || url.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var absoluteUri))
        {
            return true;
        }

        if (absoluteUri.Scheme is not ("http" or "https"))
        {
            return false;
        }

        var request = _httpContextAccessor.HttpContext?.Request;
        return request is not null
            && string.Equals(absoluteUri.Host, request.Host.Host, StringComparison.OrdinalIgnoreCase)
            && absoluteUri.Port == request.Host.Port.GetValueOrDefault(absoluteUri.Port)
            && string.Equals(absoluteUri.Scheme, request.Scheme, StringComparison.OrdinalIgnoreCase);
    }
}
