using System.Text.Json;
using System.Text.Json.Nodes;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;

namespace UmbracoCms.Web.Infrastructure.NotificationHandlers;

public class SlikiHomeContentNotificationHandler : INotificationHandler<UmbracoApplicationStartedNotification>
{
    private const string Culture = "en-US";
    private const string PageHomeAlias = "pageHome";
    private const string TitleAlias = "title";
    private const string IntroAlias = "intro";
    private const string ContentBlocksAlias = "contentBlocks";
    private const string SampleNodeName = "Test Page";
    private const string SamplePageTitle = "Test title";
    private const string SampleIntro = "Test intro";
    private const string SampleTextMarker = "test text";
    private const string NestedBlock2ColumnContentTypeKey = "277e3f8a-21af-4d83-8af7-e53ec5cb171a";
    private const string SlikiNodeName = "Sliki";
    private const string SlikiPageTitle = "Sliki";
    private const string SlikiIntro = "Upload images to the /sliki container and browse the library from one place.";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IContentService _contentService;
    private readonly ILogger<SlikiHomeContentNotificationHandler> _logger;

    public SlikiHomeContentNotificationHandler(
        IContentService contentService,
        ILogger<SlikiHomeContentNotificationHandler> logger)
    {
        _contentService = contentService;
        _logger = logger;
    }

    public void Handle(UmbracoApplicationStartedNotification notification)
    {
        IContent? homePage = _contentService
            .GetRootContent()
            .FirstOrDefault(content => content.ContentType.Alias.Equals(PageHomeAlias, StringComparison.OrdinalIgnoreCase));

        if (homePage is null)
        {
            return;
        }

        string? contentBlocks = GetStringValue(homePage, ContentBlocksAlias);
        if (!NeedsCleanup(homePage, contentBlocks))
        {
            return;
        }

        string cleanedContentBlocks = RemoveSampleBlocks(contentBlocks);

        homePage.SetCultureName(SlikiNodeName, Culture);
        homePage.SetValue(TitleAlias, SlikiPageTitle, Culture);
        homePage.SetValue(IntroAlias, SlikiIntro, Culture);
        homePage.SetValue(ContentBlocksAlias, cleanedContentBlocks, Culture);

        var publishResult = _contentService.Publish(homePage, [Culture]);
        if (!publishResult.Success)
        {
            _logger.LogWarning("Failed to publish the cleaned Sliki homepage content.");
        }
    }

    private static bool NeedsCleanup(IContent content, string? contentBlocks)
    {
        string? title = GetStringValue(content, TitleAlias);
        string? intro = GetStringValue(content, IntroAlias);

        return string.Equals(content.Name, SampleNodeName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(title, SamplePageTitle, StringComparison.OrdinalIgnoreCase)
            || string.Equals(intro, SampleIntro, StringComparison.OrdinalIgnoreCase)
            || (contentBlocks?.Contains(SampleTextMarker, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private static string? GetStringValue(IContent content, string alias)
    {
        return content.GetValue(alias, Culture)?.ToString()
            ?? content.GetValue(alias)?.ToString();
    }

    private static string RemoveSampleBlocks(string? contentBlocks)
    {
        if (string.IsNullOrWhiteSpace(contentBlocks))
        {
            return string.Empty;
        }

        try
        {
            JsonNode? root = JsonNode.Parse(contentBlocks);
            JsonArray? contentData = root?["contentData"]?.AsArray();
            JsonArray? settingsData = root?["settingsData"]?.AsArray();
            JsonArray? expose = root?["expose"]?.AsArray();
            JsonArray? layout = root?["Layout"]?["Umbraco.BlockList"]?.AsArray();

            if (contentData is null || settingsData is null || expose is null || layout is null)
            {
                return contentBlocks;
            }

            HashSet<string> removedContentKeys = [];
            HashSet<string> removedSettingsKeys = [];

            for (int index = contentData.Count - 1; index >= 0; index--)
            {
                JsonNode? contentItem = contentData[index];
                string? contentTypeKey = contentItem?["contentTypeKey"]?.GetValue<string>();
                if (!string.Equals(contentTypeKey, NestedBlock2ColumnContentTypeKey, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string? contentKey = contentItem?["key"]?.GetValue<string>();
                if (!string.IsNullOrWhiteSpace(contentKey))
                {
                    removedContentKeys.Add(contentKey);
                }

                contentData.RemoveAt(index);
            }

            if (removedContentKeys.Count == 0)
            {
                return contentBlocks;
            }

            for (int index = layout.Count - 1; index >= 0; index--)
            {
                JsonNode? layoutItem = layout[index];
                string? contentKey = layoutItem?["contentKey"]?.GetValue<string>();
                if (contentKey is null || !removedContentKeys.Contains(contentKey))
                {
                    continue;
                }

                string? settingsKey = layoutItem?["settingsKey"]?.GetValue<string>();
                if (!string.IsNullOrWhiteSpace(settingsKey))
                {
                    removedSettingsKeys.Add(settingsKey);
                }

                layout.RemoveAt(index);
            }

            for (int index = settingsData.Count - 1; index >= 0; index--)
            {
                JsonNode? settingsItem = settingsData[index];
                string? settingsKey = settingsItem?["key"]?.GetValue<string>();
                if (settingsKey is not null && removedSettingsKeys.Contains(settingsKey))
                {
                    settingsData.RemoveAt(index);
                }
            }

            for (int index = expose.Count - 1; index >= 0; index--)
            {
                JsonNode? exposeItem = expose[index];
                string? contentKey = exposeItem?["contentKey"]?.GetValue<string>();
                if (contentKey is not null && removedContentKeys.Contains(contentKey))
                {
                    expose.RemoveAt(index);
                }
            }

            return root!.ToJsonString(SerializerOptions);
        }
        catch (JsonException exception)
        {
            _ = exception;
            return contentBlocks;
        }
    }
}
