using UmbracoCms.Web.Helpers.Aliases;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Web.Common.PublishedModels;

namespace UmbracoCms.Web.Components.BasePage;

public class SocialSharing : ViewComponentExtended
{
    private static readonly IReadOnlyList<SocialChannel> SocialChannelPlatforms =
    [
        new() { Id = "facebook", Label = TranslationAliases.Common.SocialSharing.Facebook, Icon = SvgAliases.Social.Facebook },
        new() { Id = "twitter", Label = TranslationAliases.Common.SocialSharing.Twitter, Icon = SvgAliases.Social.Twitter },
        new() { Id = "linkedin", Label = TranslationAliases.Common.SocialSharing.Linkedin, Icon = SvgAliases.Social.Linkedin },
        new() { Id = "whatsapp", Label = TranslationAliases.Common.SocialSharing.Whatsapp, Icon = SvgAliases.Social.Whatsapp, CssClasses = "social-share__list-item--whatsapp" },
        new() { Id = "email", Label = TranslationAliases.Common.SocialSharing.Email, Icon = SvgAliases.Social.Email },
    ];

    public required List<SocialChannel> SocialChannels { get; set; }

    public IViewComponentResult Invoke(IPublishedContent page)
    {
        if (page is not ICompositionSocialSharingOptions socialSharingOptions)
        {
            return Content("");
        }

        SocialChannels = SocialChannelPlatforms
            .Where(socialChannel => socialSharingOptions.DisableSocialShare?.Any(s => s.Equals(socialChannel.Id, StringComparison.OrdinalIgnoreCase)) != true)
            .ToList();

        return View("~/Components/BasePage/SocialSharing.cshtml", this);
    }
}
