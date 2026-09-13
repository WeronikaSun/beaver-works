using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BeaverWorks.Desktop.ViewModels;

namespace BeaverWorks.Desktop.Views;

/// <summary>
/// Interaction logic for PlanCanvasView.xaml. Forwards raw control
/// dimensions/click coordinates to the view model, which owns all of the
/// letterbox math via <see cref="BeaverWorks.Core.Services.PlanCoordinateMapper"/>.
/// </summary>
public partial class PlanCanvasView : UserControl
{
    public PlanCanvasView()
    {
        InitializeComponent();
    }

    private void RootGrid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (DataContext is PlanCanvasViewModel viewModel)
        {
            viewModel.UpdateControlSize(RootGrid.ActualWidth, RootGrid.ActualHeight);
        }
    }

    private void PlanImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is PlanCanvasViewModel viewModel)
        {
            var position = e.GetPosition(RootGrid);
            viewModel.HandleClickCommand.Execute(position);
        }
    }
}
