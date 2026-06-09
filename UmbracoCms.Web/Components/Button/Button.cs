namespace UmbracoCms.Web.Components;

public class Button
{
    public string Element { get; set; } = "a";
    public required string Label { get; set; }
    public string? Url { get; set; }
    public string? Class { get; set; }
    public string? Variant { get; set; }
    public string? Size { get; set; }
    public string? Icon { get; set; }
    public string? Controls { get; set; }
    public Dictionary<string, string?> Attributes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string? Type { get; set; }
    public string? Hook { get; set; }
    public string? Target { get; set; }
}
