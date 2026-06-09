using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace UmbracoCms.Web.Utilities;

public static class AccessTokenUrlHelper
{
    public static string AppendToken(string url, string queryParameterName, string? token) =>
        string.IsNullOrWhiteSpace(token)
            ? url
            : QueryHelpers.AddQueryString(url, queryParameterName, token);

    public static string? GetTokenFromAbsoluteUri(string absoluteUri, string queryParameterName) =>
        Uri.TryCreate(absoluteUri, UriKind.Absolute, out var uri)
            ? GetTokenFromQueryString(uri.Query, queryParameterName)
            : null;

    public static string? GetTokenFromQueryString(string? queryString, string queryParameterName)
    {
        if (string.IsNullOrWhiteSpace(queryString))
        {
            return null;
        }

        var query = QueryHelpers.ParseQuery(queryString);
        return query.TryGetValue(queryParameterName, out StringValues values)
            ? values.ToString()
            : null;
    }
}
