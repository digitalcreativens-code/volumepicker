namespace VolumeGuard;

internal sealed class AppContext : ApplicationContext
{
    private readonly NotifyIcon _tray;
    private readonly MainForm _main;
    private readonly System.Windows.Forms.Timer _timer;

    public AppContext()
    {
        Watchdog.ClearCleanFlag();

        _main = new MainForm();

        _tray = new NotifyIcon
        {
            Visible = true,
            Text = "VolumeGuard",
            Icon = SystemIcons.Shield,
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("Status", null, (_, _) => ShowMain());
        menu.Items.Add("Podešavanja", null, (_, _) => OpenSettings());
        menu.Items.Add("Lock now", null, (_, _) => LockNow());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Izlaz", null, (_, _) => RequestExit());
        _tray.ContextMenuStrip = menu;
        _tray.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) ShowMain(); };

        _timer = new System.Windows.Forms.Timer { Interval = ConfigService.Instance.Data.PollMs };
        _timer.Tick += OnTick;
        _timer.Start();

        StartupService.Set(ConfigService.Instance.Data.StartupEnabled);
    }

    private void OnTick(object? s, EventArgs e)
    {
        try
        {
            _timer.Interval = Math.Clamp(ConfigService.Instance.Data.PollMs, 100, 10_000);

            var (maxPct, label) = ScheduleService.GetLimit(DateTime.Now);
            var maxScalar = maxPct / 100f;

            if (ConfigService.Instance.Data.ProtectionOn)
                AudioService.ClampAll(maxScalar);

            var cur = AudioService.GetMasterVolume();
            _main.UpdateStatus(maxPct, cur, label);
            _tray.Text = $"VolumeGuard — {maxPct}%";
        }
        catch (Exception ex)
        {
            ErrorLog.Write(ex);
        }
    }

    private void ShowMain()
    {
        _main.Show();
        _main.BringToFront();
    }

    private void OpenSettings()
    {
        if (!PasswordDialog.VerifyPassword()) return;
        using var f = new SettingsForm();
        f.ShowDialog();
    }

    private void LockNow()
    {
        ConfigService.Instance.Data.ProtectionOn = true;
        ConfigService.Instance.Save();
        OnTick(null, EventArgs.Empty);
    }

    private void RequestExit()
    {
        if (!PasswordDialog.VerifyPassword()) return;
        Watchdog.MarkCleanExit();
        _tray.Visible = false;
        _tray.Dispose();
        _timer.Stop();
        _timer.Dispose();
        Application.Exit();
    }
}
