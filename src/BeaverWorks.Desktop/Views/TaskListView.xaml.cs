using System.Windows.Controls;
using BeaverWorks.Core.Models;
using BeaverWorks.Desktop.ViewModels;

namespace BeaverWorks.Desktop.Views;

/// <summary>
/// Interaction logic for TaskListView.xaml. The status dropdown is bound
/// one-way to the selected task's current status (so the model stays the
/// single source of truth) and forwards the user's pick to
/// <see cref="TaskListViewModel.ChangeStatusCommand"/>, which no-ops if it
/// matches the task's current status — this also makes it safe for the
/// dropdown to re-fire when the selected task itself changes.
/// </summary>
public partial class TaskListView : UserControl
{
    public TaskListView()
    {
        InitializeComponent();
    }

    private void StatusComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is TaskListViewModel viewModel && StatusComboBox.SelectedItem is RenovationTaskStatus status)
        {
            viewModel.ChangeStatusCommand.Execute(status);
        }
    }
}
