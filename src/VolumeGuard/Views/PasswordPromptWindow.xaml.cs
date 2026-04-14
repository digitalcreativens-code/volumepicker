using System.Windows;

namespace VolumeGuard.Views;

public partial class PasswordPromptWindow : Window
{
    public string Password { get; private set; } = "";

    public PasswordPromptWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => PwdBox.Focus();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        Password = PwdBox.Password;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
