namespace ResumeBuilder.Core.Models;

public class TemplateSettings
{
    public string AccentColor { get; set; } = "#2563eb";
    public string SecondaryColor { get; set; } = "#64748b";
    public string TextColor { get; set; } = "#1f2937";
    public string HeadingColor { get; set; } = "#111827";

    public string FontFamily { get; set; } = "Arial";
    public string HeadingFontFamily { get; set; } = "Arial";

    public float FontSizeScale { get; set; } = 1.0f;
    public float LineSpacing { get; set; } = 1.4f;
    public float SectionSpacing { get; set; } = 15f;
    public float PageMargin { get; set; } = 30f;

    public static TemplateSettings Default => new();

    public static string[] AvailableFonts => new[]
    {
        "Arial",
        "Times New Roman",
        "Georgia",
        "Calibri",
        "Helvetica",
        "Verdana",
        "Trebuchet MS",
        "Garamond",
        "Palatino",
        "Century Gothic"
    };

    public static string[] PresetColors => new[]
    {
        "#2563eb", // Blue
        "#dc2626", // Red
        "#16a34a", // Green
        "#9333ea", // Purple
        "#ea580c", // Orange
        "#0891b2", // Cyan
        "#4f46e5", // Indigo
        "#be185d", // Pink
        "#1e3a5f", // Navy
        "#374151", // Gray
        "#000000", // Black
        "#0f766e"  // Teal
    };
}
