using System.Globalization;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace UmbracoCms.Web.Helpers.Extensions;

public static class PublishedElementExtensions
{
    public static string GetViewComponentName(this IPublishedElement publishedElement)
    {
        return publishedElement.ContentType.Alias.ToFirstUpper(CultureInfo.InvariantCulture);
    }
}
