using System.Windows.Controls;
using BeaverWorks.Desktop.ViewModels;

namespace BeaverWorks.Desktop.Views;

/// <summary>
/// Interaction logic for LoginView.xaml. WPF's <see cref="PasswordBox"/>
/// deliberately does not support data binding on its Password property (to
/// avoid keeping plaintext passwords in bindable state); this code-behind
/// pushes the value into the view model on every change instead.
/// </summary>
public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
    }

    private void PasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel viewModel)
        {
            viewModel.Password = PasswordBox.Password;
        }
    }
}
