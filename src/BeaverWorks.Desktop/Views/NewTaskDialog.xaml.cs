using System.Windows;
using BeaverWorks.Desktop.ViewModels;

namespace BeaverWorks.Desktop.Views;

/// <summary>
/// Interaction logic for NewTaskDialog.xaml
/// </summary>
public partial class NewTaskDialog : Window
{
    public NewTaskDialog(NewTaskViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
        viewModel.TaskCreated += (_, _) => DialogResult = true;
        viewModel.Cancelled += (_, _) => DialogResult = false;
    }
}
