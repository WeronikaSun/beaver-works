using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BeaverWorks.Desktop.Views;

/// <summary>
/// Collapses a bound element when the source string is null or empty;
/// otherwise shows it. Used to hide error/status messages until they have
/// content.
/// </summary>
public sealed class NullOrEmptyStringToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        var text = value as string;
        return string.IsNullOrEmpty(text) ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
