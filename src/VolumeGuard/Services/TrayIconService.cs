using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using VolumeGuard.Views;
using Application = System.Windows.Application;

namespace VolumeGuard.Services;

public sealed class TrayIconService : IDisposable
{
    private readonly ConfigService _config;
    private readonly StartupService _startup;
    private readonly VolumeEnforcementService _enforcement;
    private readonly AudioService _audio;
    private readonly ScheduleService _schedule;
    private NotifyIcon? _icon;
    private Window? _main;

    public TrayIconService(
        ConfigService config,
        StartupService startup,
        VolumeEnforcementService enforcement,
        AudioService audio,
        ScheduleService schedule)
    {
        _config = config;
        _startup = startup;
        _enforcement = enforcement;
        _audio = audio;
        _schedule = schedule;
    }

    public void Initialize(Window mainWindow)
    {
        _main = mainWindow;

        _icon = new NotifyIcon
        {
            Visible = true,
            Text = "VolumeGuard",
            Icon = SystemIcons.Shield
        };

        _icon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
                Application.Current.Dispatcher.Invoke(ShowMain);
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("Status", null, (_, _) => Application.Current.Dispatcher.Invoke(ShowMain));
        menu.Items.Add("Settings", null, (_, _) => Application.Current.Dispatcher.Invoke(OpenSettings));
        menu.Items.Add("Lock now", null, (_, _) => Application.Current.Dispatcher.Invoke(LockNow));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => Application.Current.Dispatcher.Invoke(RequestExit));

        _icon.ContextMenuStrip = menu;
    }

    private void ShowMain()
    {
        if (_main == null) return;
        _main.Show();
        _main.WindowState = WindowState.Normal;
        _main.Activate();
    }

    private void OpenSettings()
    {
        if (!PasswordGate.VerifyOrCancel()) return;

        var sp = App.Services ?? throw new InvalidOperationException("Services not initialized.");
        var w = sp.GetRequiredService<SettingsWindow>();
        w.Owner = _main;
        w.ShowDialog();
    }

    private void LockNow()
    {
        _config.Current.ProtectionEnabled = true;
        _config.Save();
        try
        {
            var now = TimeOnly.FromDateTime(DateTime.Now);
            var (max, _) = _schedule.GetAllowedMaximum(now);
            _audio.ClampAllEndpointsToMax(max);
        }
        catch
        {
            // ignore
        }
    }

    private void RequestExit()
    {
        if (!PasswordGate.VerifyOrCancel()) return;

        App.SuppressPasswordForClose = true;
        App.MarkNextShutdownAsClean = true;
        Application.Current.Shutdown();
    }

    public void Dispose()
    {
        if (_icon != null)
        {
            _icon.Visible = false;
            _icon.Dispose();
            _icon = null;
        }
    }
}
