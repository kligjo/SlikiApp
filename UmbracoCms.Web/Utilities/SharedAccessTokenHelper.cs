using System.Security.Cryptography;
using System.Text;
using UmbracoCms.Web.Infrastructure.Configuration.Options;

namespace UmbracoCms.Web.Utilities;

public static class SharedAccessTokenHelper
{
    public static bool TryResolveRequestToken(HttpContext context, AccessTokenOptions options, out string token)
    {
        token = context.Request.Query[options.QueryParameterName].ToString() ?? "";
        if (!string.IsNullOrWhiteSpace(token))
        {
            return true;
        }

        var refererHeader = context.Request.Headers.Referer.ToString();
        if (string.IsNullOrWhiteSpace(refererHeader)
            || !Uri.TryCreate(refererHeader, UriKind.Absolute, out var refererUri))
        {
            return false;
        }

        if (!string.Equals(refererUri.Host, context.Request.Host.Host, StringComparison.OrdinalIgnoreCase)
            || refererUri.Port != context.Request.Host.Port.GetValueOrDefault(refererUri.Port)
            || !string.Equals(refererUri.Scheme, context.Request.Scheme, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        token = AccessTokenUrlHelper.GetTokenFromQueryString(refererUri.Query, options.QueryParameterName) ?? string.Empty;
        return !string.IsNullOrEmpty(token);
    }

    public static bool TokensMatch(string actualToken, string expectedToken)
    {
        var actualBytes = Encoding.UTF8.GetBytes(actualToken);
        var expectedBytes = Encoding.UTF8.GetBytes(expectedToken);

        return actualBytes.Length == expectedBytes.Length
            && CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
    }
}
