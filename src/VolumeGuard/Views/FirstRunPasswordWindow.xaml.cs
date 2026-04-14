using System.Windows;
using WpfMessageBox = System.Windows.MessageBox;

namespace VolumeGuard.Views;

public partial class FirstRunPasswordWindow : Window
{
    public string Password { get; private set; } = "";

    public FirstRunPasswordWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => Pwd1.Focus();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (Pwd1.Password.Length < 4)
        {
            WpfMessageBox.Show("Šifra mora imati bar 4 karaktera.", "VolumeGuard", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (Pwd1.Password != Pwd2.Password)
        {
            WpfMessageBox.Show("Šifre se ne poklapaju.", "VolumeGuard", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Password = Pwd1.Password;
        DialogResult = true;
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
