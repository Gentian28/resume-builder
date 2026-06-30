namespace ResumeBuilder.Core.Models;

public class TemplateInfo
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required TemplateCategory Category { get; init; }
    public required TemplateLayout Layout { get; init; }
    public string[] Tags { get; init; } = Array.Empty<string>();
    public string DefaultAccentColor { get; init; } = "#2563eb";
    public string DefaultFontFamily { get; init; } = "Inter";
}

public enum TemplateCategory
{
    Professional,
    Creative,
    Modern,
    Classic,
    Technical,
    Academic
}

public enum TemplateLayout
{
    SingleColumn,
    TwoColumn,
    Sidebar
}
