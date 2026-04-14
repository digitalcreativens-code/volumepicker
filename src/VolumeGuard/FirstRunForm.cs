namespace VolumeGuard;

internal sealed class FirstRunForm : Form
{
    private readonly TextBox _p1, _p2;

    public FirstRunForm()
    {
        Text = "VolumeGuard — postavi šifru";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        Size = new Size(380, 260);
        BackColor = Color.FromArgb(30, 30, 30);
        ForeColor = Color.White;

        var info = new Label
        {
            Text = "Postavi master šifru.\nBiće potrebna za izlazak, podešavanja i isključivanje auto-starta.",
            Left = 20, Top = 15, Width = 330, Height = 40, AutoSize = false
        };

        var l1 = new Label { Text = "Šifra:", Left = 20, Top = 65, AutoSize = true };
        _p1 = new TextBox
        {
            Left = 20, Top = 85, Width = 320, UseSystemPasswordChar = true,
            BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White
        };

        var l2 = new Label { Text = "Ponovi:", Left = 20, Top = 120, AutoSize = true };
        _p2 = new TextBox
        {
            Left = 20, Top = 140, Width = 320, UseSystemPasswordChar = true,
            BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White
        };

        var save = new Button { Text = "Sačuvaj", Left = 170, Top = 180, Width = 80, DialogResult = DialogResult.None };
        var exit = new Button { Text = "Izađi", Left = 260, Top = 180, Width = 80, DialogResult = DialogResult.Cancel };

        save.FlatStyle = FlatStyle.Flat;
        exit.FlatStyle = FlatStyle.Flat;

        save.Click += (_, _) =>
        {
            if (_p1.Text.Length < 4)
            {
                MessageBox.Show("Šifra mora imati bar 4 karaktera.", "VolumeGuard",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (_p1.Text != _p2.Text)
            {
                MessageBox.Show("Šifre se ne poklapaju.", "VolumeGuard",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            PasswordService.Instance.SetPassword(_p1.Text);
            DialogResult = DialogResult.OK;
            Close();
        };

        AcceptButton = save;
        CancelButton = exit;
        Controls.AddRange(new Control[] { info, l1, _p1, l2, _p2, save, exit });
        Shown += (_, _) => _p1.Focus();
    }
}
