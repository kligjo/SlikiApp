using UmbracoCms.Web.Models.Globalization;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace UmbracoCms.Web.Services;

public interface IGlobalizationService
{
    List<AlternateUrl> GetAlternateUrls(IPublishedContent content);
}
