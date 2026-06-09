using System.Text.Json;
using System.Text.Json.Serialization;
using Umbraco.Cms.Web.Common.PublishedModels;
using NestedBlockVideoModel = Umbraco.Cms.Web.Common.PublishedModels.NestedBlockVideo;

namespace UmbracoCms.Web.Components;

public class Video
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerOptions.Default) { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    public required string InstanceId { get; set; }
    public required string Platform { get; set; }
    public string? Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public Image? Thumbnail { get; set; }
    public string? TotalTime { get; set; }
    public string? StartTime { get; set; }
    public string? Classes { get; set; }
    public bool Muted { get; set; }
    public bool Autoplay { get; set; }
    public bool AutoPause { get; set; } = true;
    public bool PlaysInLine { get; set; } = true;
    public bool Controls { get; set; } = true;
    public bool Loop { get; set; } = true;
    private IEnumerable<VideoSizeSource>? Sources { get; init; }
    public string? SourcesJson => Sources?.Any() == true ? JsonSerializer.Serialize(Sources, JsonOptions) : null;
    public string? ClosedCaptions { get; set; }
    public string? EmbedUrl { get; set; }
    public string? UploadDate { get; set; }
    public bool InView { get; set; } = true;

    public static Video? Create(NestedBlockVideoModel block, string? css = null)
    {
        return new Video
        {
            InstanceId = $"{Random.Shared.Next()}",
            Platform = "native",
            //TO DO: Map sources
            //Title = block.Name,
            Classes = css,
            Thumbnail = Image.Create(block as Umbraco.Cms.Core.Models.PublishedContent.IPublishedContent, 720, 400, "video__image"),
        };
    }
}
