using UmbracoCms.Web.Helpers;
using UmbracoCms.Web.Helpers.Extensions;
using UmbracoCms.Web.Infrastructure.Configuration.Options;
using UmbracoCms.Web.Infrastructure.DependencyInjection;
using UmbracoCms.Web.Models.Globalization;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;

namespace UmbracoCms.Web.Services;

[Transient(typeof(IGlobalizationService))]
public class GlobalizationService : IGlobalizationService
{
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;
    private readonly IPublishedUrlProvider _publishedUrlProvider;
    private readonly IVariationContextAccessor _variationContextAccessor;
    private readonly IOptionsMonitor<ApplicationOptions> _applicationOptions;

    public GlobalizationService(
        IUmbracoContextAccessor umbracoContextAccessor,
        IPublishedUrlProvider publishedUrlProvider,
        IVariationContextAccessor variationContextAccessor,
        IOptionsMonitor<ApplicationOptions> applicationOptions)
    {
        _umbracoContextAccessor = umbracoContextAccessor;
        _publishedUrlProvider = publishedUrlProvider;
        _variationContextAccessor = variationContextAccessor;
        _applicationOptions = applicationOptions;
    }

    public List<AlternateUrl> GetAlternateUrls(IPublishedContent content)
    {
        List<AlternateUrl> alternateUrls = [];
        if (content.Cultures.Count <= 1)
        {
            return alternateUrls;
        }

        string? defaultCulture = content.Cultures.Keys.FirstOrDefault();

        foreach ((string culture, _) in content.Cultures)
        {
            if (!IsPublishedAndRoutable(content, culture))
            {
                continue;
            }

            using var variationHelper = new VariationContextHelper(_variationContextAccessor, culture);
            string? url = content.Url(culture, UrlMode.Absolute);

            if (url is null or "#")
            {
                continue;
            }

            alternateUrls.Add(new AlternateUrl
            {
                Lang = culture,
                Url = url,
                IsDefault = culture == defaultCulture,
            });
        }

        return alternateUrls;
    }

    private bool IsPublishedAndRoutable(IPublishedContent content, string culture)
    {
        if (!content.IsPublished(culture))
        {
            return false;
        }

        using var variationHelper = new VariationContextHelper(_variationContextAccessor, culture);
        string? url = content.Url(culture, UrlMode.Absolute);

        if (url is null or "#")
        {
            return false;
        }

        if (!_applicationOptions.CurrentValue.IsCrawlableUrl(new Uri(url)))
        {
            return false;
        }

        return true;
    }
}
