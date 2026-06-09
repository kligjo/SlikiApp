using SimpleMvcSitemap;

namespace UmbracoCms.Web.Services;

public interface ISitemapService
{
    List<SitemapNode> GenerateSitemap(string culture);
    SitemapIndexNode[] GenerateSitemapIndex(IEnumerable<(string Culture, Uri DomainUri)> domains);
    List<SitemapNode> GenerateEmptySitemap();
    IEnumerable<(string Culture, Uri DomainUri)> GetDomainsForHost(string authority);
}
