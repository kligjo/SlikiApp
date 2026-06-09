namespace UmbracoCms.Web.Infrastructure.Configuration.Options;

public class DevelopmentOptions
{
    public List<string> AssetsFallbackDirectories { get; set; } = [];

    public Uri? AssetsFallbackUri { get; set; }

    public List<string> AssetsSubdirectories { get; set; } = [];

    public bool DeveloperExceptionPage { get; set; }
}
