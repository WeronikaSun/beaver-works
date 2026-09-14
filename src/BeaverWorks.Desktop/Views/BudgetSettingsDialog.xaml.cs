using System.Windows;
using BeaverWorks.Desktop.ViewModels;

namespace BeaverWorks.Desktop.Views;

/// <summary>
/// Interaction logic for BudgetSettingsDialog.xaml
/// </summary>
public partial class BudgetSettingsDialog : Window
{
    public BudgetSettingsDialog(BudgetSettingsViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
        viewModel.BudgetUpdated += (_, _) => DialogResult = true;
        viewModel.Cancelled += (_, _) => DialogResult = false;
    }
}
