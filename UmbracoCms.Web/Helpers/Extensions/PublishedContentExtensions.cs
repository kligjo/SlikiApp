using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Web.Common.PublishedModels;

namespace UmbracoCms.Web.Helpers.Extensions;

public static class PublishedContentExtensions
{
    [return: NotNullIfNotNull(nameof(defaultValue))]
    public static TValue? ValueOrDefault<TModel, TValue>(
        this TModel content,
        Expression<Func<TModel, TValue>> property,
        TValue defaultValue)
        where TModel : IPublishedElement
    {
        return content.ValueFor(property, fallback: Fallback.ToDefaultValue, defaultValue: defaultValue);
    }

    public static string GetTitle(this IPublishedContent content)
    {
        if (content is ICompositionBasePage { Title: { } title } && !string.IsNullOrEmpty(title))
        {
            return title;
        }
        return content.Name ?? "";
    }

    public static bool IsFolder(this IPublishedContent node)
    {
        return node.ContentType.Alias.EndsWith("folder", StringComparison.OrdinalIgnoreCase);
    }
}
