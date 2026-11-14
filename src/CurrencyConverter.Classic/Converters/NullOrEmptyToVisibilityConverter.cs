using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CurrencyConverter.Classic.Converters;

/// <summary>
/// Collapsed when the value is null or an empty string, Visible otherwise.
/// Used to show the validation message only when there is one.
/// </summary>
[ValueConversion(typeof(object), typeof(Visibility))]
public sealed class NullOrEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is null || (value is string text && text.Length == 0)
            ? Visibility.Collapsed
            : Visibility.Visible;

    // One-way only: the View never pushes a Visibility back to the ViewModel.
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
