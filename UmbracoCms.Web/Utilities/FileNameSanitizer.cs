using System.Text;
using System.Text.RegularExpressions;

namespace UmbracoCms.Web.Utilities;

public static partial class FileNameSanitizer
{
    public static string SanitizeStem(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "image";
        }

        var builder = new StringBuilder(value.Length);

        foreach (var character in value.Trim())
        {
            if (char.IsLetterOrDigit(character) || character is '-' or '_')
            {
                builder.Append(character);
            }
            else if (char.IsWhiteSpace(character) || character is '.')
            {
                builder.Append('-');
            }
        }

        var sanitized = MultiDashRegex().Replace(builder.ToString(), "-").Trim('-');
        return string.IsNullOrWhiteSpace(sanitized)
            ? "image"
            : sanitized[..Math.Min(sanitized.Length, 80)];
    }

    public static string SanitizeDisplayName(string? value)
    {
        var fileName = Path.GetFileName(value ?? string.Empty);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            return "image";
        }

        foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(invalidCharacter, '-');
        }

        return MultiDashRegex().Replace(fileName, "-");
    }

    public static string? GetExtensionForContentType(string contentType) =>
        contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/gif" => ".gif",
            "image/webp" => ".webp",
            "image/bmp" => ".bmp",
            _ => null
        };

    [GeneratedRegex("-{2,}")]
    private static partial Regex MultiDashRegex();
}
