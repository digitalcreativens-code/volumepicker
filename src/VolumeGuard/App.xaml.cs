using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using VolumeGuard.Services;
using VolumeGuard.ViewModels;
using VolumeGuard.Views;
using WpfMessageBox = System.Windows.MessageBox;

namespace VolumeGuard;

public partial class App : System.Windows.Application
{
    private static Mutex? _mainMutex;

    public static bool MarkNextShutdownAsClean { get; set; }
    public static bool SuppressPasswordForClose { get; set; }
    internal static IServiceProvider? Services { get; private set; }

    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "VolumeGuard", "error.log");

    protected override void OnStartup(StartupEventArgs e)
    {
        // Global exception handlers — show error and write log instead of silent crash
        DispatcherUnhandledException += (_, ex) =>
        {
            WriteLog(ex.Exception);
            WpfMessageBox.Show(
                $"Neočekivana greška:\n\n{ex.Exception.Message}\n\nDetalji su u: {LogPath}",
                "VolumeGuard — Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            ex.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (_, ex) =>
        {
            if (ex.ExceptionObject is Exception exc)
                WriteLog(exc);
        };

        try
        {
            _mainMutex = new Mutex(true, AppConstants.MainMutexName, out var createdNew);
            if (!createdNew)
            {
                WpfMessageBox.Show("VolumeGuard je već pokrenut.", "VolumeGuard",
                    MessageBoxButton.OK, MessageBoxImage.Information);
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
            var sp = Services;

            var config = sp.GetRequiredService<ConfigService>();
            config.Load();

            var startup = sp.GetRequiredService<StartupService>();
            var exePath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exePath))
            {
                try { startup.SetEnabled(config.Current.StartupEnabled, exePath); }
                catch { /* non-fatal */ }
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
        catch (Exception ex)
        {
            WriteLog(ex);
            WpfMessageBox.Show(
                $"Greška pri pokretanju:\n\n{ex.Message}\n\nDetalji u: {LogPath}",
                "VolumeGuard — Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private static void WriteLog(Exception ex)
    {
        try
        {
            var dir = Path.GetDirectoryName(LogPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.AppendAllText(LogPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}\n\n");
        }
        catch { /* ignore log failures */ }
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
        catch { /* ignore */ }

        try
        {
            Services?.GetService<VolumeEnforcementService>()?.Stop();
            Services?.GetService<TrayIconService>()?.Dispose();
            Services?.GetService<AudioService>()?.Dispose();
        }
        catch { /* ignore */ }

        try
        {
            if (_mainMutex != null)
            {
                _mainMutex.ReleaseMutex();
                _mainMutex.Dispose();
                _mainMutex = null;
            }
        }
        catch { /* ignore */ }

        base.OnExit(e);
    }
}
