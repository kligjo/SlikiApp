using UmbracoCms.Web.Helpers.Aliases;
using Umbraco.Cms.Web.Common.PublishedModels;

namespace UmbracoCms.Web.Components.BasePage;

public class SocialLinks
{
    private static readonly IReadOnlyList<(string Domain, string Icon)> SocialLinkPlatforms =
    [
        ("facebook.com", SvgAliases.Social.Facebook),
        ("twitter.com", SvgAliases.Social.Twitter),
        ("linkedin.com", SvgAliases.Social.Linkedin),
        ("instagram.com", SvgAliases.Social.Instagram),
        ("youtube.com", SvgAliases.Social.Youtube),
    ];

    public required List<Link> Links { get; set; }

    public static SocialLinks? Create(SiteSettings? settings)
    {
        if (settings is not ICompositionSocialLinks socialLinks || socialLinks.SocialLinks?.Any() != true)
        {
            return null;
        }

        List<Link> links = socialLinks.SocialLinks
            .Select(GetSocialLink)
            .WhereNotNull()
            .ToList();

        return new SocialLinks { Links = links };
    }

    private static Link? GetSocialLink(Umbraco.Cms.Core.Models.Link? link)
    {
        if (link?.Url is null or "")
        {
            return null;
        }

        if (!Uri.TryCreate(link.Url, UriKind.Absolute, out Uri? uri))
        {
            return null;
        }

        (string Domain, string Icon) socialPlatform = SocialLinkPlatforms
            .FirstOrDefault(s => uri.Host.EndsWith(s.Domain, StringComparison.OrdinalIgnoreCase));

        return socialPlatform == default
            ? null
            : Link.Create(link, icon: socialPlatform.Icon, hideLabel: true);
    }
}
