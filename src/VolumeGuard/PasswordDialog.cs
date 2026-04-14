namespace VolumeGuard;

internal sealed class PasswordDialog : Form
{
    private readonly TextBox _txt;

    public string EnteredPassword => _txt.Text;

    public PasswordDialog()
    {
        Text = "Unesite šifru";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        Size = new Size(340, 170);
        BackColor = Color.FromArgb(30, 30, 30);
        ForeColor = Color.White;

        var lbl = new Label { Text = "Master šifra:", Left = 20, Top = 20, AutoSize = true };
        _txt = new TextBox
        {
            Left = 20, Top = 45, Width = 280, UseSystemPasswordChar = true,
            BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White
        };

        var ok = new Button { Text = "OK", Left = 120, Top = 85, Width = 80, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "Otkaži", Left = 210, Top = 85, Width = 80, DialogResult = DialogResult.Cancel };

        ok.FlatStyle = FlatStyle.Flat;
        cancel.FlatStyle = FlatStyle.Flat;

        AcceptButton = ok;
        CancelButton = cancel;
        Controls.AddRange(new Control[] { lbl, _txt, ok, cancel });

        Shown += (_, _) => _txt.Focus();
    }

    public static bool VerifyPassword()
    {
        using var dlg = new PasswordDialog();
        if (dlg.ShowDialog() != DialogResult.OK) return false;
        if (!PasswordService.Instance.Verify(dlg.EnteredPassword))
        {
            MessageBox.Show("Pogrešna šifra.", "VolumeGuard",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
        return true;
    }
}
