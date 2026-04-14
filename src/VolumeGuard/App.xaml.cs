using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using VolumeGuard.Services;
using VolumeGuard.ViewModels;
using VolumeGuard.Views;

namespace VolumeGuard;

public partial class App : Application
{
    private static Mutex? _mainMutex;

    /// <summary>Set to true immediately before <see cref="Application.Shutdown"/> after password-approved exit.</summary>
    public static bool MarkNextShutdownAsClean { get; set; }

    /// <summary>Avoid re-entrancy password prompts when shutdown was already authorized (e.g. tray Exit).</summary>
    public static bool SuppressPasswordForClose { get; set; }

    internal static IServiceProvider? Services { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        _mainMutex = new Mutex(true, AppConstants.MainMutexName, out var createdNew);
        if (!createdNew)
        {
            MessageBox.Show("VolumeGuard je već pokrenut.", "VolumeGuard", MessageBoxButton.OK, MessageBoxImage.Information);
            _mainMutex.Dispose();
            _mainMutex = null;
            Shutdown();
            return;
        }

        CleanExitTracker.ClearOnStartup();
        SuppressPasswordForClose = false;
        MarkNextShutdownAsClean = false;

        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();
        var sp = Services ?? throw new InvalidOperationException("DI container missing.");

        var config = sp.GetRequiredService<ConfigService>();
        config.Load();

        var startup = sp.GetRequiredService<StartupService>();
        var exePath = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(exePath))
        {
            try
            {
                startup.SetEnabled(config.Current.StartupEnabled, exePath);
            }
            catch
            {
                // non-fatal — user can fix from Settings later
            }
        }

        var password = sp.GetRequiredService<PasswordService>();
        password.LoadFromConfig(config.Current.PasswordHash);

        if (!password.HasPassword)
        {
            var setup = new FirstRunPasswordWindow();
            if (setup.ShowDialog() != true || string.IsNullOrEmpty(setup.Password))
            {
                Shutdown();
                return;
            }

            password.SetPassword(setup.Password);
            config.Current.PasswordHash = password.Hash!;
            config.Save();
        }

        WatchdogRunner.EnsureWatchdogRunning();

        var main = sp.GetRequiredService<MainWindow>();
        Current.MainWindow = main;
        main.Show();

        sp.GetRequiredService<TrayIconService>().Initialize(main);
        sp.GetRequiredService<VolumeEnforcementService>().Start();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ConfigService>();
        services.AddSingleton<PasswordService>();
        services.AddSingleton<AudioService>();
        services.AddSingleton<ScheduleService>();
        services.AddSingleton<StartupService>();
        services.AddSingleton<VolumeEnforcementService>();
        services.AddSingleton<TrayIconService>();
        services.AddSingleton<MainWindow>();
        services.AddTransient<SettingsWindow>();
        services.AddTransient<PasswordPromptWindow>();
        services.AddSingleton<StatusViewModel>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            if (MarkNextShutdownAsClean)
            {
                CleanExitTracker.MarkCleanExit();
                MarkNextShutdownAsClean = false;
            }
        }
        catch
        {
            // ignore
        }

        try
        {
            Services?.GetService<VolumeEnforcementService>()?.Stop();
            Services?.GetService<TrayIconService>()?.Dispose();
            Services?.GetService<AudioService>()?.Dispose();
        }
        catch
        {
            // ignore shutdown errors
        }

        try
        {
            if (_mainMutex != null)
            {
                _mainMutex.ReleaseMutex();
                _mainMutex.Dispose();
                _mainMutex = null;
            }
        }
        catch
        {
            // ignore
        }

        base.OnExit(e);
    }
}
