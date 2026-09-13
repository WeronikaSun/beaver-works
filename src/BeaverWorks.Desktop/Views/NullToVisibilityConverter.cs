using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BeaverWorks.Desktop.Views;

/// <summary>
/// Collapses a bound element when the source object is null; otherwise
/// shows it. Used to hide the task details panel until a task is selected.
/// </summary>
public sealed class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is null ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
