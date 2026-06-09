using UmbracoCms.Web.Infrastructure.Configuration.Options;
using Umbraco.Cms.Core.Sync;

namespace UmbracoCms.Web.Helpers;

public static class CrawlableDomainsHelper
{
    public static bool IsCrawlableUrl(this ApplicationOptions applicationOptions, Uri absoluteUri)
    {
        return applicationOptions.IsCrawlableDomain(absoluteUri.Host);
    }

    private static bool IsCrawlableDomain(this ApplicationOptions applicationOptions, string host)
    {
        if (applicationOptions.CrawlableDomains.Any(d => d == "*"))
        {
            return true;
        }

        if (host is null or "")
        {
            return false;
        }

        return applicationOptions.CrawlableDomains.Any(d => d == host)
               && applicationOptions.ServerRole is ServerRole.Single or ServerRole.Subscriber;
    }
}
