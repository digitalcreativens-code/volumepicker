namespace VolumeGuard;

internal sealed class MainForm : Form
{
    private readonly Label _lblProtection, _lblAllowed, _lblCurrent, _lblSlot, _lblWatchdog, _lblStartup;

    public MainForm()
    {
        Text = "VolumeGuard";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        Size = new Size(460, 350);
        BackColor = Color.FromArgb(30, 30, 30);
        ForeColor = Color.White;

        var title = new Label { Text = "VolumeGuard", Font = new Font("Segoe UI", 16, FontStyle.Bold), Left = 20, Top = 15, AutoSize = true };
        var sub = new Label { Text = "Kontrola maksimalnog volume-a po rasporedu.", Left = 20, Top = 48, AutoSize = true, ForeColor = Color.Gray };

        int y = 90, gap = 32;
        _lblProtection = AddRow("Zaštita:", ref y, gap);
        _lblAllowed = AddRow("Dozvoljeni maks:", ref y, gap);
        _lblCurrent = AddRow("Trenutni volume:", ref y, gap);
        _lblSlot = AddRow("Aktivni slot:", ref y, gap);
        _lblWatchdog = AddRow("Watchdog:", ref y, gap);
        _lblStartup = AddRow("Auto-start:", ref y, gap);

        Controls.Add(title);
        Controls.Add(sub);
    }

    private Label AddRow(string caption, ref int y, int gap)
    {
        var lbl = new Label { Text = caption, Left = 20, Top = y, Width = 160, ForeColor = Color.Gray };
        var val = new Label { Text = "—", Left = 185, Top = y, Width = 250, AutoSize = false };
        Controls.Add(lbl);
        Controls.Add(val);
        y += gap;
        return val;
    }

    public void UpdateStatus(int allowedPct, float currentScalar, string slotLabel)
    {
        if (InvokeRequired) { BeginInvoke(() => UpdateStatus(allowedPct, currentScalar, slotLabel)); return; }

        var cfg = ConfigService.Instance.Data;
        _lblProtection.Text = cfg.ProtectionOn ? "Uključena" : "Isključena";
        _lblProtection.ForeColor = cfg.ProtectionOn ? Color.LimeGreen : Color.OrangeRed;
        _lblAllowed.Text = $"{allowedPct}%";
        _lblCurrent.Text = $"{(int)(currentScalar * 100)}%";
        _lblSlot.Text = slotLabel;
        _lblWatchdog.Text = Watchdog.IsRunning() ? "Da" : "Ne";
        _lblStartup.Text = StartupService.IsEnabled() ? "Da" : "Ne";
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }
}
