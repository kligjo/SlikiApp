namespace UmbracoCms.Web.Models.Globalization;

public class AlternateUrl
{
    public required string Lang { get; set; }

    public required string Url { get; set; }

    public required bool IsDefault { get; set; }
}
