using Microsoft.Extensions.Options;
using Sliki.Web.Models;
using Sliki.Web.Options;

namespace Sliki.Web.Services;

public sealed class ImageFileValidator
{
    private static readonly Dictionary<string, string> MimeAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpg"] = "image/jpeg",
        ["image/pjpeg"] = "image/jpeg",
        ["image/x-png"] = "image/png"
    };

    private readonly BlobStorageOptions _options;
    private readonly HashSet<string> _allowedMimeTypes;

    public ImageFileValidator(IOptions<BlobStorageOptions> options)
    {
        _options = options.Value;
        _allowedMimeTypes = _options.AllowedMimeTypes
            .Select(NormalizeMimeType)
            .Where(static mimeType => !string.IsNullOrWhiteSpace(mimeType))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static readonly Dictionary<string, string> MimeToExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"]      = ".jpg,.jpeg",
        ["image/png"]       = ".png",
        ["image/gif"]       = ".gif",
        ["image/webp"]      = ".webp",
        ["image/bmp"]       = ".bmp",
        ["image/heic"]      = ".heic",
        ["image/heif"]      = ".heif",
        ["video/mp4"]       = ".mp4,.m4v",
        ["video/quicktime"] = ".mov",
        ["video/webm"]      = ".webm",
        ["video/x-msvideo"] = ".avi",
        ["application/zip"] = ".zip",
    };

    public long MaxUploadBytes => _options.MaxUploadBytes;

    public int PageSize => _options.PageSize;

    public string AcceptAttributeValue =>
        string.Join(",", _allowedMimeTypes
            .SelectMany(m => MimeToExtensions.TryGetValue(m, out var ext)
                ? (IEnumerable<string>)[m, ext]
                : [m]));

    public ImageValidationResult Validate(
        string fileName,
        string? browserContentType,
        long sizeInBytes,
        ReadOnlySpan<byte> headerBytes)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return ImageValidationResult.Failure("Select a file with a valid name.");
        }

        if (sizeInBytes <= 0)
        {
            return ImageValidationResult.Failure("The selected file is empty.");
        }

        if (sizeInBytes > _options.MaxUploadBytes)
        {
            return ImageValidationResult.Failure(
                $"The file exceeds the configured limit of {Utilities.FileSizeFormatter.Format(_options.MaxUploadBytes)}.");
        }

        var normalizedBrowserContentType = NormalizeMimeType(browserContentType);
        if (!string.IsNullOrWhiteSpace(normalizedBrowserContentType)
            && !_allowedMimeTypes.Contains(normalizedBrowserContentType))
        {
            return ImageValidationResult.Failure("The selected file type is not allowed.");
        }

        if (!TryDetectMimeType(headerBytes, out var detectedMimeType)
            || !_allowedMimeTypes.Contains(detectedMimeType))
        {
            return ImageValidationResult.Failure("The file type is not supported.");
        }

        if (!string.IsNullOrWhiteSpace(normalizedBrowserContentType)
            && !string.Equals(normalizedBrowserContentType, detectedMimeType, StringComparison.OrdinalIgnoreCase))
        {
            return ImageValidationResult.Failure("The file MIME type does not match the image content.");
        }

        return ImageValidationResult.Success(detectedMimeType);
    }

    private static string NormalizeMimeType(string? mimeType)
    {
        if (string.IsNullOrWhiteSpace(mimeType))
        {
            return string.Empty;
        }

        return MimeAliases.TryGetValue(mimeType.Trim(), out var normalizedMimeType)
            ? normalizedMimeType
            : mimeType.Trim();
    }

    private static bool TryDetectMimeType(ReadOnlySpan<byte> headerBytes, out string mimeType)
    {
        if (headerBytes.Length >= 3
            && headerBytes[0] == 0xFF
            && headerBytes[1] == 0xD8
            && headerBytes[2] == 0xFF)
        {
            mimeType = "image/jpeg";
            return true;
        }

        if (headerBytes.Length >= 8
            && headerBytes[0] == 0x89
            && headerBytes[1] == 0x50
            && headerBytes[2] == 0x4E
            && headerBytes[3] == 0x47
            && headerBytes[4] == 0x0D
            && headerBytes[5] == 0x0A
            && headerBytes[6] == 0x1A
            && headerBytes[7] == 0x0A)
        {
            mimeType = "image/png";
            return true;
        }

        if (headerBytes.Length >= 6
            && headerBytes[..6].SequenceEqual("GIF87a"u8))
        {
            mimeType = "image/gif";
            return true;
        }

        if (headerBytes.Length >= 6
            && headerBytes[..6].SequenceEqual("GIF89a"u8))
        {
            mimeType = "image/gif";
            return true;
        }

        if (headerBytes.Length >= 12
            && headerBytes[..4].SequenceEqual("RIFF"u8)
            && headerBytes[8..12].SequenceEqual("WEBP"u8))
        {
            mimeType = "image/webp";
            return true;
        }

        if (headerBytes.Length >= 2
            && headerBytes[..2].SequenceEqual("BM"u8))
        {
            mimeType = "image/bmp";
            return true;
        }

        // WebM / MKV
        if (headerBytes.Length >= 4
            && headerBytes[0] == 0x1A
            && headerBytes[1] == 0x45
            && headerBytes[2] == 0xDF
            && headerBytes[3] == 0xA3)
        {
            mimeType = "video/webm";
            return true;
        }

        // AVI
        if (headerBytes.Length >= 12
            && headerBytes[..4].SequenceEqual("RIFF"u8)
            && headerBytes[8..12].SequenceEqual("AVI "u8))
        {
            mimeType = "video/x-msvideo";
            return true;
        }

        // HEIC / HEIF — ftyp box with HEIC/HEIF brands
        if (headerBytes.Length >= 12
            && headerBytes[4..8].SequenceEqual("ftyp"u8))
        {
            var brand = headerBytes[8..12];
            if (brand.SequenceEqual("heic"u8) || brand.SequenceEqual("heix"u8) ||
                brand.SequenceEqual("hevc"u8) || brand.SequenceEqual("hevx"u8))
            {
                mimeType = "image/heic";
                return true;
            }
            if (brand.SequenceEqual("mif1"u8) || brand.SequenceEqual("msf1"u8))
            {
                mimeType = "image/heif";
                return true;
            }
            // MOV / MP4
            mimeType = brand.SequenceEqual("qt  "u8) ? "video/quicktime" : "video/mp4";
            return true;
        }

        // ZIP
        if (headerBytes.Length >= 4
            && headerBytes[0] == 0x50
            && headerBytes[1] == 0x4B
            && headerBytes[2] == 0x03
            && headerBytes[3] == 0x04)
        {
            mimeType = "application/zip";
            return true;
        }

        mimeType = string.Empty;
        return false;
    }
}
