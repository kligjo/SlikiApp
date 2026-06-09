using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using Asp.Versioning;
using UmbracoCms.Web.Helpers;
using UmbracoCms.Web.Helpers.Extensions;
using UmbracoCms.Web.Infrastructure.Configuration.Options;
using UmbracoCms.Web.Services;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SimpleMvcSitemap;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Web;

namespace UmbracoCms.Web.Api.Controllers;

/// <summary>
/// Represents the sitemap controller.
/// </summary>
[ApiVersionNeutral]
[Route("[controller]")]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class SitemapController : ApiControllerBase, IDisposable
{
    private readonly ISitemapService _sitemapService;
    private readonly IOptionsMonitor<ApplicationOptions> _applicationOptions;
    private UmbracoContextReference _umbracoContextReference;

    public SitemapController(
        ISitemapService sitemapService,
        IUmbracoContextFactory umbracoContextFactory,
        IOptionsMonitor<ApplicationOptions> applicationOptions)
    {
        _sitemapService = sitemapService;
        _applicationOptions = applicationOptions;

        // Using IUmbracoContextFactory since the umbraco context is not available
        // This only happens because our routes have file extensions
        _umbracoContextReference = umbracoContextFactory.EnsureUmbracoContext();
    }

    /// <summary>
    /// Generates the website sitemap.
    /// </summary>
    [HttpGet]
    [Route("~/sitemap.xml")]
    [Produces(MediaTypeNames.Text.Xml)]
    public IActionResult Index()
    {
        Uri currentUri = new(Request.GetDisplayUrl());
        if (!_applicationOptions.CurrentValue.IsCrawlableUrl(currentUri))
        {
            return new SitemapProvider().CreateSitemap(new SitemapModel(_sitemapService.GenerateEmptySitemap()));
        }

        var domains = _sitemapService.GetDomainsForHost(currentUri.Authority).ToList();

        return domains.Count switch
        {
            0 or 1 => new SitemapProvider().CreateSitemap(new SitemapModel(_sitemapService.GenerateSitemap(domains.FirstOrDefault().Culture ?? "en"))),
            _ => new SitemapProvider().CreateSitemapIndex(new SitemapIndexModel(_sitemapService.GenerateSitemapIndex(domains).ToList())),
        };
    }

    [HttpGet]
    [Route("{culture}.xml")]
    [Produces(MediaTypeNames.Text.Xml)]
    public IActionResult CultureSpecific([FromRoute, Required] string culture)
    {
        Uri currentUri = new(Request.GetDisplayUrl());
        if (!_applicationOptions.CurrentValue.IsCrawlableUrl(currentUri))
        {
            return NotFound();
        }

        return new SitemapProvider().CreateSitemap(new SitemapModel(_sitemapService.GenerateSitemap(culture)));
    }

    public void Dispose()
    {
        _umbracoContextReference?.Dispose();
        _umbracoContextReference = null!;
    }
}
