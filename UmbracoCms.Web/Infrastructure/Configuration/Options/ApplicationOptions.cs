using System.ComponentModel.DataAnnotations;
using Umbraco.Cms.Core.Sync;

namespace UmbracoCms.Web.Infrastructure.Configuration.Options;

public class ApplicationOptions
{
    public enum HostingEnvironmentType
    {
        Default = 0,
        AzureWebApp = 1,
    }

    public CacheOptions Cache { get; set; } = new();

    public CorsOptions Cors { get; set; } = new();

    public DevelopmentOptions Development { get; set; } = new();

    public HostingEnvironmentType HostingEnvironment { get; set; } = HostingEnvironmentType.Default;

    [EnumDataType(typeof(ServerRole), ErrorMessage = "ServerRole must not be Unknown")]
    public ServerRole ServerRole { get; set; } = ServerRole.Single;

    public List<string> CrawlableDomains { get; set; } = [];

    public bool EnableCriticalCss { get; set; }
}

public class CorsOptions
{
    public string[] Origins { get; set; } = [];
    public string[] Methods { get; set; } = [];
    public string[] Headers { get; set; } = [];
}
