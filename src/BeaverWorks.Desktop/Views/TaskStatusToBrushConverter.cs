using System.Globalization;
using System.Windows.Data;
using BeaverWorks.Core.Models;
using BeaverWorks.Desktop.ViewModels;

namespace BeaverWorks.Desktop.Views;

/// <summary>
/// Maps a <see cref="RenovationTaskStatus"/> to its FR-008 status color so the
/// task list rows show the same swatch as the canvas markers
/// (<see cref="TaskStatusColors"/>).
/// </summary>
public sealed class TaskStatusToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is RenovationTaskStatus status
            ? TaskStatusColors.ForStatus(status)
            : System.Windows.Media.Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
