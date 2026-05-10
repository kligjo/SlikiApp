namespace Sliki.Web.Utilities;

public static class FileSizeFormatter
{
    private static readonly string[] Units = ["bytes", "KB", "MB", "GB"];

    public static string Format(long value)
    {
        if (value < 1024)
        {
            return $"{value} bytes";
        }

        var size = value;
        var unitIndex = 0;

        while (size >= 1024 && unitIndex < Units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        var preciseValue = value / Math.Pow(1024, unitIndex);
        return $"{preciseValue:0.#} {Units[unitIndex]}";
    }
}
