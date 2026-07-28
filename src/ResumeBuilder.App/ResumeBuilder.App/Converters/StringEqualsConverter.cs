using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ResumeBuilder.App.Converters;

/// <summary>
/// True when the bound string equals the converter parameter.
///
/// Used to show exactly one editor section at a time without giving the view model a boolean
/// property per section, which would be sixteen properties that must all raise change
/// notifications whenever the selection moves.
/// </summary>
public sealed class StringEqualsConverter : IValueConverter
{
    public static readonly StringEqualsConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => string.Equals(value as string, parameter as string, StringComparison.Ordinal);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
