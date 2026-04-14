using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WpfMessageBox = System.Windows.MessageBox;
using VolumeGuard.Models;
using VolumeGuard.Services;

namespace VolumeGuard.Views;

public partial class SettingsWindow : Window
{
    private readonly ConfigService _config;
    private readonly StartupService _startup;
    private readonly ObservableCollection<ScheduleSlot> _slots = new();

    public SettingsWindow(ConfigService config, StartupService startup)
    {
        InitializeComponent();
        _config = config;
        _startup = startup;

        foreach (var s in _config.Current.ScheduleSlots)
            _slots.Add(new ScheduleSlot { Start = s.Start, End = s.End, MaxVolumePercent = s.MaxVolumePercent });

        GridSlots.ItemsSource = _slots;
        GridSlots.Columns.Add(new DataGridTextColumn { Header = "Start (HH:mm)", Binding = new Binding("Start"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
        GridSlots.Columns.Add(new DataGridTextColumn { Header = "Kraj (HH:mm)", Binding = new Binding("End"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
        GridSlots.Columns.Add(new DataGridTextColumn { Header = "Max %", Binding = new Binding("MaxVolumePercent"), Width = 120 });

        TxtPoll.Text = _config.Current.PollingIntervalMs.ToString();
        ChkProtection.IsChecked = _config.Current.ProtectionEnabled;
        ChkStartup.IsChecked = _startup.IsEnabled();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(TxtPoll.Text, out var poll) || poll < 100 || poll > 10_000)
        {
            WpfMessageBox.Show("Interval mora biti broj između 100 i 10000 ms.", "VolumeGuard", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _config.Current.PollingIntervalMs = poll;
        _config.Current.ProtectionEnabled = ChkProtection.IsChecked == true;
        _config.Current.ScheduleSlots = _slots.ToList();

        var exe = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exe))
        {
            WpfMessageBox.Show("Ne mogu da odredim putanju EXE fajla.", "VolumeGuard", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            _startup.SetEnabled(ChkStartup.IsChecked == true, exe);
            _config.Current.StartupEnabled = ChkStartup.IsChecked == true;
        }
        catch (Exception ex)
        {
            WpfMessageBox.Show("Greška pri upisu auto-starta: " + ex.Message, "VolumeGuard", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        _config.Save();
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
