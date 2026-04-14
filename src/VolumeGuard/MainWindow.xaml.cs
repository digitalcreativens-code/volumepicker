using System.Windows;
using System.Windows.Threading;
using VolumeGuard.Services;
using VolumeGuard.ViewModels;
using VolumeGuard.Views;

namespace VolumeGuard;

public partial class MainWindow : Window
{
    private readonly StatusViewModel _vm;
    private readonly VolumeEnforcementService _enforcement;
    private readonly ConfigService _config;
    private readonly StartupService _startup;
    private readonly DispatcherTimer _heartbeat;

    public MainWindow(
        StatusViewModel vm,
        VolumeEnforcementService enforcement,
        ConfigService config,
        StartupService startup)
    {
        InitializeComponent();
        _vm = vm;
        _enforcement = enforcement;
        _config = config;
        _startup = startup;
        DataContext = _vm;

        _enforcement.Tick += OnEnforcementTick;

        _heartbeat = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _heartbeat.Tick += (_, _) => RefreshAuxiliaryStatus();
        _heartbeat.Start();

        Loaded += (_, _) =>
        {
            RefreshAuxiliaryStatus();
            Hide();
        };

        Closing += MainWindow_Closing;
    }

    private void OnEnforcementTick(double allowed, double actual, string desc)
    {
        Dispatcher.BeginInvoke(() =>
        {
            _vm.AllowedPercent = allowed * 100.0;
            _vm.CurrentPercent = actual * 100.0;
            _vm.SlotDescription = desc;
            _vm.ProtectionEnabled = _config.Current.ProtectionEnabled;
        });
    }

    private void RefreshAuxiliaryStatus()
    {
        _vm.WatchdogAlive = WatchdogRunner.IsWatchdogRunning();
        _vm.StartupEnabled = _startup.IsEnabled();
        WatchdogRunner.EnsureWatchdogRunning();
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (App.SuppressPasswordForClose)
            return;

        e.Cancel = true;
        if (!PasswordGate.VerifyOrCancel())
            return;

        App.SuppressPasswordForClose = true;
        App.MarkNextShutdownAsClean = true;
        System.Windows.Application.Current.Shutdown();
    }

    protected override void OnClosed(EventArgs e)
    {
        _enforcement.Tick -= OnEnforcementTick;
        _heartbeat.Stop();
        base.OnClosed(e);
    }
}
