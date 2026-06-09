namespace UmbracoCms.Web.Components;

public partial class LanguageSwitch
{
    public class Language
    {
        public required string Label { get; set; }
        public required string CultureCode { get; init; }
        public required string LanguageCode { get; init; }
        public required string Url { get; set; }
        public required string Icon { get; init; }
        public bool IsActive { get; init; }
    }
}
