namespace ResumeBuilder.Core.Models;

public class TemplateSettings
{
    public const string DefaultAccentColor = "#2563eb";
    public const string DefaultFontFamily = "Arial";

    public string AccentColor { get; set; } = DefaultAccentColor;
    public string SecondaryColor { get; set; } = "#64748b";
    public string TextColor { get; set; } = "#1f2937";
    public string HeadingColor { get; set; } = "#111827";

    public string FontFamily { get; set; } = DefaultFontFamily;
    public string HeadingFontFamily { get; set; } = DefaultFontFamily;

    public float FontSizeScale { get; set; } = 1.0f;
    public float LineSpacing { get; set; } = 1.4f;
    public float SectionSpacing { get; set; } = 15f;
    public float PageMargin { get; set; } = 30f;

    /// <summary>
    /// True once the user has explicitly picked an accent color. Until then the selected
    /// template's <see cref="TemplateInfo.DefaultAccentColor"/> wins.
    /// </summary>
    public bool IsAccentColorCustomized { get; set; }

    /// <summary>
    /// True once the user has explicitly picked a font. Until then the selected
    /// template's <see cref="TemplateInfo.DefaultFontFamily"/> wins.
    /// </summary>
    public bool IsFontCustomized { get; set; }

    /// <summary>
    /// Applies a template's defaults for any styling the user has not explicitly customized.
    /// </summary>
    public void ApplyTemplateDefaults(TemplateInfo template)
    {
        if (!IsAccentColorCustomized)
        {
            AccentColor = template.DefaultAccentColor;
        }

        if (!IsFontCustomized)
        {
            FontFamily = template.DefaultFontFamily;
            HeadingFontFamily = template.DefaultFontFamily;
        }
    }

    public TemplateSettings Clone() => (TemplateSettings)MemberwiseClone();

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
