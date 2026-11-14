using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CurrencyConverter.Converters;

/// <summary>
/// true → Visible, false → Collapsed (or the opposite when <see cref="IsInverted"/> is set).
/// Lets the ViewModel expose plain bools (IsLoading, HasResult) instead of WPF's Visibility type.
/// </summary>
[ValueConversion(typeof(bool), typeof(Visibility))]
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public bool IsInverted { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isVisible = value is true;
        if (IsInverted)
        {
            isVisible = !isVisible;
        }

        return isVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value is Visibility.Visible) != IsInverted;
}
