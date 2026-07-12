using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Templates;

internal static class TemplateColors
{
    private static readonly Color DefaultAccent = ParseHex(TemplateSettings.DefaultAccentColor) ?? Colors.Blue.Medium;

    public static Color Parse(string? hex) => ParseHex(hex) ?? DefaultAccent;

    /// <summary>
    /// Accepts #RGB, #RRGGBB and 8-digit hex. An 8-digit value is read as #AARRGGBB, which is the
    /// convention QuestPDF itself uses; #RRGGBBAA cannot be told apart from it by length alone.
    /// </summary>
    private static Color? ParseHex(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return null;

        var value = hex.Trim();
        if (value.StartsWith('#'))
            value = value[1..];

        if (!value.All(Uri.IsHexDigit))
            return null;

        try
        {
            switch (value.Length)
            {
                case 3:
                    var r3 = (byte)(Convert.ToByte(value[0].ToString(), 16) * 17);
                    var g3 = (byte)(Convert.ToByte(value[1].ToString(), 16) * 17);
                    var b3 = (byte)(Convert.ToByte(value[2].ToString(), 16) * 17);
                    return Color.FromRGB(r3, g3, b3);

                case 6:
                    return Color.FromRGB(
                        Convert.ToByte(value.Substring(0, 2), 16),
                        Convert.ToByte(value.Substring(2, 2), 16),
                        Convert.ToByte(value.Substring(4, 2), 16));

                case 8:
                    return Color.FromARGB(
                        Convert.ToByte(value.Substring(0, 2), 16),
                        Convert.ToByte(value.Substring(2, 2), 16),
                        Convert.ToByte(value.Substring(4, 2), 16),
                        Convert.ToByte(value.Substring(6, 2), 16));

                default:
                    return null;
            }
        }
        catch
        {
            return null;
        }
    }
}
