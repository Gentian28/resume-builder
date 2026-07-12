namespace ResumeBuilder.Core.Models;

/// <summary>
/// Converts between the editor's "one achievement per line" text box and the model's list.
/// Both directions live here because the two must agree exactly: a tailored edit refers to an
/// achievement by its index in the model, and that index is only meaningful if the line-to-index
/// mapping used to write it back is the same one used to read it out.
/// </summary>
public static class AchievementLines
{
    private static readonly string[] LineBreaks = { "\r\n", "\n", "\r" };

    /// <summary>Splits editor text into achievements, dropping blank lines.</summary>
    public static List<string> Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        return text
            .Split(LineBreaks, StringSplitOptions.None)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Trim())
            .ToList();
    }

    /// <summary>Joins achievements back into editor text.</summary>
    public static string Format(IEnumerable<string> achievements) =>
        string.Join("\n", achievements);

    /// <summary>
    /// Replaces the achievement at <paramref name="index"/> (counting only the lines
    /// <see cref="Parse"/> would keep) and returns the updated editor text. Blank lines the user
    /// left in the box are preserved in place.
    /// </summary>
    public static string ReplaceAt(string? text, int index, string value)
    {
        if (index < 0)
            return text ?? string.Empty;

        var lines = (text ?? string.Empty).Split(LineBreaks, StringSplitOptions.None);
        var remaining = index;

        for (var i = 0; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            if (remaining == 0)
            {
                lines[i] = value;
                return string.Join("\n", lines);
            }

            remaining--;
        }

        return string.Join("\n", lines);
    }
}
