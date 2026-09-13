using System.Windows;
using BeaverWorks.Desktop.ViewModels;

namespace BeaverWorks.Desktop.Views;

/// <summary>
/// Interaction logic for NewProjectDialog.xaml
/// </summary>
public partial class NewProjectDialog : Window
{
    public NewProjectDialog(NewProjectViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
        viewModel.ProjectCreated += (_, _) => DialogResult = true;
        viewModel.Cancelled += (_, _) => DialogResult = false;
    }
}
