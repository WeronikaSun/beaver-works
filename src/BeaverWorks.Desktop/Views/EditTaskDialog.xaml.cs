using System.Windows;
using BeaverWorks.Desktop.ViewModels;

namespace BeaverWorks.Desktop.Views;

/// <summary>
/// Interaction logic for EditTaskDialog.xaml
/// </summary>
public partial class EditTaskDialog : Window
{
    public EditTaskDialog(EditTaskViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
        viewModel.TaskUpdated += (_, _) => DialogResult = true;
        viewModel.Cancelled += (_, _) => DialogResult = false;
    }
}
