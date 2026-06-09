using UmbracoCms.Web.Helpers.Extensions;
using UmbracoCms.Web.Models.Globalization;
using UmbracoCms.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Web.Common.PublishedModels;

namespace UmbracoCms.Web.Components.BasePage;

public class MetaTags : ViewComponentExtended
{
    private const string SampleTitle = "Test title";
    private const string SampleIntro = "Test intro";
    private const string DefaultSlikiTitle = "Sliki";
    private const string DefaultSlikiIntro = "Upload images to the /sliki container and browse the library from one place.";

    private readonly IGlobalizationService _globalizationService;

    public MetaTags(IGlobalizationService globalizationService)
    {
        _globalizationService = globalizationService;
    }

    public required string Title { get; set; }
    public required string Description { get; set; }
    public required string Robots { get; set; }
    public required string Language { get; set; }
    public string? CanonicalUrl { get; set; }
    public required List<AlternateUrl> AlternateLanguageUrls { get; set; }
    public required OpenGraphMetaTags OpenGraph { get; set; }
    public required TwitterMetaTags Twitter { get; set; }
    public string? ApplicationName { get; set; }

    public IViewComponentResult Invoke(IPublishedContent page, SiteSettings? siteSettings)
    {
        bool isErrorPage = HttpContext.Response.StatusCode is < 200 or > 299;
        string? websiteName = siteSettings?.WebsiteName;

        Title = GetTitle(page, websiteName);
        Description = GetMetaDescription(page);
        Robots = GetRobots(page, isErrorPage);
        Language = System.Globalization.CultureInfo.CurrentCulture.ToString();
        CanonicalUrl = !isErrorPage ? page.Url(mode: UrlMode.Absolute) : null;
        AlternateLanguageUrls = !isErrorPage ? _globalizationService.GetAlternateUrls(page) : [];

        OpenGraph = GetOpenGraph(page);
        Twitter = GetTwitter(page);
        ApplicationName = siteSettings?.WebsiteName;

        return View("~/Components/BasePage/MetaTags.cshtml", this);
    }

    private static string GetTitle(IPublishedContent page, string? websiteNameSuffix = null)
    {
        string title = NormalizeTitle((page as ICompositionSeo)?.SeoMetaTitle.NullOrEmptyAsNull() ?? page.GetTitle());
        if (!string.IsNullOrEmpty(websiteNameSuffix))
        {
            title = title.EnsureEndsWith($" | {websiteNameSuffix}");
        }
        return title;
    }

    private static string GetMetaDescription(IPublishedContent page)
    {
        string? metaDescription = NormalizeIntro((page as ICompositionSeo)?.SeoMetaDescription);
        if (!metaDescription.IsNullOrEmpty()) return metaDescription;
        string? pageIntro = NormalizeIntro((page as ICompositionBasePage)?.Intro)?.RemoveHtml().TruncateOnWholeWord(156);
        return pageIntro ?? "";
    }

    private static string NormalizeTitle(string? title)
    {
        string normalized = title?.Trim() ?? string.Empty;
        return string.Equals(normalized, SampleTitle, StringComparison.OrdinalIgnoreCase)
            ? DefaultSlikiTitle
            : normalized;
    }

    private static string NormalizeIntro(string? intro)
    {
        string normalized = intro?.Trim() ?? string.Empty;
        return string.Equals(normalized, SampleIntro, StringComparison.OrdinalIgnoreCase)
            ? DefaultSlikiIntro
            : normalized;
    }

    private static string GetRobots(IPublishedContent page, bool isErrorPage)
    {
        if (isErrorPage) return "noindex,nofollow";
        if (page is not ICompositionSeo compositionSeo) return "index,follow";
        string metaFollowContent = compositionSeo.DoNotFollow ? "nofollow" : "follow";
        string metaIndexContent = compositionSeo.DoNotIndex ? "noindex" : "index";
        return $"{metaIndexContent},{metaFollowContent}";
    }

    private static OpenGraphMetaTags GetOpenGraph(IPublishedContent page)
    {
        ICompositionSocialSharing? socialSharing = page as ICompositionSocialSharing;
        string ogTitle = socialSharing?.OgTitle.NullOrEmptyAsNull() ?? GetTitle(page);
        string ogDescription = socialSharing?.OgDescription.NullOrEmptyAsNull() ?? GetMetaDescription(page);
        MediaWithCrops? ogImage = socialSharing?.OgImage ?? (page as ICompositionHero)?.HeroImage;
        string? ogImageUrl = (ogImage?.Content as Umbraco.Cms.Web.Common.PublishedModels.Image)?.GetDefaultCropUrl(1200, 630, urlMode: UrlMode.Absolute);
        return new OpenGraphMetaTags { Title = ogTitle, Description = ogDescription, ImageUrl = ogImageUrl };
    }

    private static TwitterMetaTags GetTwitter(IPublishedContent page)
    {
        ICompositionSocialSharing? socialSharing = page as ICompositionSocialSharing;
        string twitterTitle = socialSharing?.TwitterTitle.NullOrEmptyAsNull() ?? socialSharing?.OgTitle.NullOrEmptyAsNull() ?? GetTitle(page);
        string twitterDescription = socialSharing?.TwitterDescription.NullOrEmptyAsNull() ?? socialSharing?.OgDescription.NullOrEmptyAsNull() ?? GetMetaDescription(page);
        MediaWithCrops? twitterImage = socialSharing?.TwitterImage ?? socialSharing?.OgImage ?? (page as ICompositionHero)?.HeroImage;
        string? twitterImageUrl = (twitterImage?.Content as Umbraco.Cms.Web.Common.PublishedModels.Image)?.GetDefaultCropUrl(1200, 630, urlMode: UrlMode.Absolute);
        return new TwitterMetaTags { Title = twitterTitle, Description = twitterDescription, ImageUrl = twitterImageUrl };
    }
}
