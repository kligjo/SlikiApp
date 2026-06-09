using UmbracoCms.Web.Helpers.Aliases;

namespace UmbracoCms.Web.Components.BasePage;

public class SocialChannel
{
    public required string Id { get; set; }
    public required TranslationEntry Label { get; set; }
    public required string Icon { get; set; }
    public string? CssClasses { get; set; }
}
