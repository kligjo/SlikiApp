using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.RegularExpressions;

namespace UmbracoCms.Web.Helpers.Extensions;

public static partial class StringExtensions
{
    public static bool IsNullOrEmpty([NotNullWhen(false)] this string? value)
    {
        return string.IsNullOrEmpty(value);
    }

    public static string? NullOrEmptyAsNull(this string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    [return: NotNullIfNotNull(nameof(fallBack))]
    public static string? FallBack(this string? firstString, string? fallBack)
    {
        return string.IsNullOrEmpty(firstString) ? fallBack : firstString;
    }

    public static string RemoveHtml(this string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }
        text = text.StripHtml();
        text = MultipleConsecutiveSpacesRegex().Replace(text, " ");
        return WebUtility.HtmlDecode(text);
    }

    public static string TruncateOnWholeWord(this string input, int length, string suffix = "...")
    {
        if (length <= 0 || string.IsNullOrEmpty(input) || input.Length <= length)
        {
            return input;
        }
        string trimmedString = input[..Math.Max(Math.Min(length - suffix.Length, input.Length), 0)];
        return trimmedString.LastIndexOf(' ') is var lastSpaceIndex and >= 0
            ? $"{input[..lastSpaceIndex]}{suffix}"
            : $"{trimmedString}{suffix}";
    }

    [GeneratedRegex(@"\\s{2,}")]
    private static partial Regex MultipleConsecutiveSpacesRegex();
}
