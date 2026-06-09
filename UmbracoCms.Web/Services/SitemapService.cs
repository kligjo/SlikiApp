using UmbracoCms.Web.Helpers.Extensions;
using UmbracoCms.Web.Infrastructure.Configuration.Options;
using UmbracoCms.Web.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Options;
using SimpleMvcSitemap;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common;
using Umbraco.Cms.Web.Common.PublishedModels;

namespace UmbracoCms.Web.Services;

[Transient(typeof(ISitemapService))]
public class SitemapService : ISitemapService
{
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;
    private readonly IPublishedUrlProvider _publishedUrlProvider;
    private readonly IVariationContextAccessor _variationContextAccessor;
    private readonly IOptionsMonitor<ApplicationOptions> _applicationOptions;
    private readonly UmbracoHelper _umbracoHelper;

    public SitemapService(
        IUmbracoContextAccessor umbracoContextAccessor,
        IPublishedUrlProvider publishedUrlProvider,
        IVariationContextAccessor variationContextAccessor,
        IOptionsMonitor<ApplicationOptions> applicationOptions,
        UmbracoHelper umbracoHelper)
    {
        _umbracoContextAccessor = umbracoContextAccessor;
        _publishedUrlProvider = publishedUrlProvider;
        _variationContextAccessor = variationContextAccessor;
        _applicationOptions = applicationOptions;
        _umbracoHelper = umbracoHelper;
    }

    public IEnumerable<(string Culture, Uri DomainUri)> GetDomainsForHost(string authority)
    {
        if (!_umbracoContextAccessor.TryGetUmbracoContext(out var context))
        {
            return [];
        }

        return context.Domains?.GetAll(false)
            .Where(d => d.Name != null && new Uri(d.Name).Authority == authority)
            .Select(d => (Culture: d.Culture ?? "", DomainUri: new Uri(d.Name!)))
            ?? [];
    }

    public SitemapIndexNode[] GenerateSitemapIndex(IEnumerable<(string Culture, Uri DomainUri)> domains)
    {
        return domains
            .Select(d => new SitemapIndexNode(new Uri(d.DomainUri, $"/sitemap/{d.Culture}.xml").AbsoluteUri))
            .ToArray();
    }

    public List<SitemapNode> GenerateSitemap(string culture)
    {
        if (!_umbracoContextAccessor.TryGetUmbracoContext(out var context))
        {
            return [];
        }

        List<SitemapNode> nodes = [];
        var root = _umbracoHelper.ContentAtRoot()?.FirstOrDefault();
        if (root == null)
        {
            return nodes;
        }

        AddNodeRecursive(root, culture, nodes);
        return nodes;
    }

    public List<SitemapNode> GenerateEmptySitemap()
    {
        return [];
    }

    private void AddNodeRecursive(IPublishedContent content, string culture, List<SitemapNode> nodes)
    {
        if (!content.IsPublished(culture))
        {
            return;
        }

        if (content is ICompositionSeo seo && seo.DoNotIndex)
        {
            return;
        }

        string? url = content.Url(culture, UrlMode.Absolute);
        if (url is not (null or "#"))
        {
            bool isHomePage = content is PageHome;
            nodes.Add(new SitemapNode(url)
            {
                ChangeFrequency = isHomePage ? ChangeFrequency.Monthly : ChangeFrequency.Weekly,
                Priority = isHomePage ? 0.8m : 0.6m,
                LastModificationDate = content.UpdateDate,
            });
        }

        foreach (var child in content.Children(culture) ?? [])
        {
            if (!child.IsFolder())
            {
                AddNodeRecursive(child, culture, nodes);
            }
        }
    }
}
