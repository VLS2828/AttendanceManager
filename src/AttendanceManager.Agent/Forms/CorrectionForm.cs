namespace AttendanceManager.Agent.Forms;

internal class CorrectionForm : Form
{
    private readonly DateTimePicker _dtpDate;
    private readonly RadioButton _rbPresent;
    private readonly RadioButton _rbOnLeave;
    private readonly DateTimePicker _dtpLogin;
    private readonly DateTimePicker _dtpLogout;
    private readonly Panel _pnlTimes;
    private readonly TextBox _txtReason;

    public string SelectedDate => _dtpDate.Value.ToString("yyyy-MM-dd");
    public string ClaimedStatus => _rbPresent.Checked ? "Present" : "OnLeave";
    public string? ClaimedLoginTime => _rbPresent.Checked ? _dtpDate.Value.Date.Add(_dtpLogin.Value.TimeOfDay).ToString("yyyy-MM-dd HH:mm:ss") : null;
    public string? ClaimedLogoutTime => _rbPresent.Checked ? _dtpDate.Value.Date.Add(_dtpLogout.Value.TimeOfDay).ToString("yyyy-MM-dd HH:mm:ss") : null;
    public string Reason => _txtReason.Text.Trim();

    public CorrectionForm()
    {
        Text = "Request Attendance Correction";
        Width = 420;
        Height = 330;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;

        var lblDate = new Label { Text = "Date:", Left = 20, Top = 20, Width = 100, TextAlign = System.Drawing.ContentAlignment.MiddleRight };
        _dtpDate = new DateTimePicker { Left = 130, Top = 18, Width = 240, Format = DateTimePickerFormat.Short, Value = DateTime.Today };

        var lblStatus = new Label { Text = "I was:", Left = 20, Top = 55, Width = 100, TextAlign = System.Drawing.ContentAlignment.MiddleRight };
        _rbPresent = new RadioButton { Text = "Present", Left = 130, Top = 54, Width = 100, Checked = true };
        _rbOnLeave = new RadioButton { Text = "On Leave", Left = 240, Top = 54, Width = 100 };

        _pnlTimes = new Panel { Left = 0, Top = 82, Width = 400, Height = 60 };
        var lblLogin = new Label { Text = "Login Time:", Left = 20, Top = 8, Width = 100, TextAlign = System.Drawing.ContentAlignment.MiddleRight };
        _dtpLogin = new DateTimePicker { Left = 130, Top = 6, Width = 240, Format = DateTimePickerFormat.Time, ShowUpDown = true, Value = DateTime.Today.AddHours(9).AddMinutes(30) };
        var lblLogout = new Label { Text = "Logout Time:", Left = 20, Top = 36, Width = 100, TextAlign = System.Drawing.ContentAlignment.MiddleRight };
        _dtpLogout = new DateTimePicker { Left = 130, Top = 34, Width = 240, Format = DateTimePickerFormat.Time, ShowUpDown = true, Value = DateTime.Today.AddHours(18).AddMinutes(30) };
        _pnlTimes.Controls.AddRange(new Control[] { lblLogin, _dtpLogin, lblLogout, _dtpLogout });

        var lblReason = new Label { Text = "Reason:", Left = 20, Top = 152, Width = 100, TextAlign = System.Drawing.ContentAlignment.TopRight };
        _txtReason = new TextBox { Left = 130, Top = 150, Width = 240, Height = 60, Multiline = true };

        _rbPresent.CheckedChanged += (_, _) => _pnlTimes.Visible = _rbPresent.Checked;
        _rbOnLeave.CheckedChanged += (_, _) => _pnlTimes.Visible = _rbPresent.Checked;

        var btnCancel = new Button { Text = "Cancel", Left = 200, Top = 248, Width = 80, Height = 30 };
        var btnSubmit = new Button { Text = "Submit", Left = 292, Top = 248, Width = 80, Height = 30, FlatStyle = FlatStyle.Flat };
        btnSubmit.BackColor = System.Drawing.Color.FromArgb(0, 120, 212);
        btnSubmit.ForeColor = System.Drawing.Color.White;

        btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        btnSubmit.Click += OnSubmit;

        Controls.AddRange(new Control[] { lblDate, _dtpDate, lblStatus, _rbPresent, _rbOnLeave, _pnlTimes, lblReason, _txtReason, btnCancel, btnSubmit });
    }

    private void OnSubmit(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_txtReason.Text))
        {
            MessageBox.Show("Please provide a reason for the correction.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (_rbPresent.Checked && _dtpLogout.Value.TimeOfDay <= _dtpLogin.Value.TimeOfDay)
        {
            MessageBox.Show("Logout time must be after login time.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        DialogResult = DialogResult.OK;
        Close();
    }
}
