using System.Windows.Controls;

namespace BeaverWorks.Desktop.Views;

/// <summary>
/// Interaction logic for ProjectWorkspaceView.xaml. Purely a layout host
/// splitting the canvas and task list panels — both child views bind
/// directly to their own view model via <c>DataContext</c>.
/// </summary>
public partial class ProjectWorkspaceView : UserControl
{
    public ProjectWorkspaceView()
    {
        InitializeComponent();
    }
}
