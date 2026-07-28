using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ResumeBuilder.App.Converters;

/// <summary>
/// Colours the AI status dot in the command bar.
///
/// Green means a model is reachable on this machine and nothing leaves it — the product's
/// strongest claim, so it is the only state that gets the affirmative colour. Any other state is
/// neutral rather than red: not having configured a model is not an error, and keyword analysis
/// and ATS scoring work regardless.
/// </summary>
public sealed class AiStatusBrushConverter : IValueConverter
{
    public static readonly AiStatusBrushConverter Instance = new();

    private static readonly SolidColorBrush Local = new(Color.Parse("#3FB950"));
    private static readonly SolidColorBrush Other = new(Color.Parse("#868D9B"));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Local : Other;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
