using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ResumeBuilder.App.Converters;

/// <summary>
/// Expanded/collapsed as a chevron. A glyph rather than an icon asset so it inherits the text
/// colour and scales with the theme's font, and because two characters do not justify a resource.
/// </summary>
public class BoolToChevronConverter : IValueConverter
{
    public static readonly BoolToChevronConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? "⌄" : "›";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
