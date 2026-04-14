namespace VolumeGuard;

internal sealed class SettingsForm : Form
{
    private readonly DataGridView _grid;
    private readonly TextBox _txtPoll;
    private readonly CheckBox _chkProtection, _chkStartup;

    public SettingsForm()
    {
        Text = "Podešavanja";
        Size = new Size(520, 430);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(30, 30, 30);
        ForeColor = Color.White;

        var cfg = ConfigService.Instance.Data;

        var lblSch = new Label { Text = "Raspored (HH:mm):", Left = 15, Top = 10, AutoSize = true };
        _grid = new DataGridView
        {
            Left = 15, Top = 32, Width = 475, Height = 180,
            BackgroundColor = Color.FromArgb(40, 40, 40),
            ForeColor = Color.White,
            GridColor = Color.FromArgb(60, 60, 60),
            AllowUserToAddRows = true,
            AllowUserToDeleteRows = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        _grid.DefaultCellStyle.BackColor = Color.FromArgb(40, 40, 40);
        _grid.DefaultCellStyle.ForeColor = Color.White;
        _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(50, 50, 50);
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        _grid.EnableHeadersVisualStyles = false;

        _grid.Columns.Add("From", "Od (HH:mm)");
        _grid.Columns.Add("To", "Do (HH:mm)");
        _grid.Columns.Add("MaxPct", "Max %");

        foreach (var s in cfg.Slots)
            _grid.Rows.Add(s.From, s.To, s.MaxPct);

        var lblPoll = new Label { Text = "Interval (ms):", Left = 15, Top = 225, AutoSize = true };
        _txtPoll = new TextBox
        {
            Left = 120, Top = 222, Width = 80, Text = cfg.PollMs.ToString(),
            BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White
        };

        _chkProtection = new CheckBox { Text = "Zaštita aktivna", Left = 15, Top = 260, Checked = cfg.ProtectionOn, AutoSize = true };
        _chkStartup = new CheckBox { Text = "Pokreni sa Windows-om", Left = 15, Top = 290, Checked = StartupService.IsEnabled(), AutoSize = true };

        var save = new Button { Text = "Sačuvaj", Left = 310, Top = 340, Width = 80, FlatStyle = FlatStyle.Flat };
        var cancel = new Button { Text = "Otkaži", Left = 400, Top = 340, Width = 80, FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };

        save.Click += (_, _) =>
        {
            if (!int.TryParse(_txtPoll.Text, out var poll) || poll < 100 || poll > 10000)
            {
                MessageBox.Show("Interval: 100–10000 ms.", "VolumeGuard", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var slots = new List<Slot>();
            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (row.IsNewRow) continue;
                var f = row.Cells[0].Value?.ToString() ?? "";
                var t = row.Cells[1].Value?.ToString() ?? "";
                var pStr = row.Cells[2].Value?.ToString() ?? "100";
                if (!int.TryParse(pStr, out var p)) p = 100;
                if (!string.IsNullOrWhiteSpace(f) && !string.IsNullOrWhiteSpace(t))
                    slots.Add(new Slot { From = f, To = t, MaxPct = Math.Clamp(p, 0, 100) });
            }

            cfg.PollMs = poll;
            cfg.ProtectionOn = _chkProtection.Checked;
            cfg.Slots = slots;
            StartupService.Set(_chkStartup.Checked);
            cfg.StartupEnabled = _chkStartup.Checked;
            ConfigService.Instance.Save();

            DialogResult = DialogResult.OK;
            Close();
        };

        CancelButton = cancel;
        Controls.AddRange(new Control[] { lblSch, _grid, lblPoll, _txtPoll, _chkProtection, _chkStartup, save, cancel });
    }
}
