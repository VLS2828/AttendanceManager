using AttendanceManager.Shared.DTOs;

namespace AttendanceManager.Agent.Forms;

internal class LeaveRequestForm : Form
{
    private readonly ComboBox _cmbLeaveType;
    private readonly DateTimePicker _dtpStart;
    private readonly DateTimePicker _dtpEnd;
    private readonly Label _lblTotalDays;
    private readonly TextBox _txtReason;

    public int SelectedLeaveTypeId => ((LeaveTypeDto)_cmbLeaveType.SelectedItem!).Id;
    public string StartDate => _dtpStart.Value.ToString("yyyy-MM-dd");
    public string EndDate => _dtpEnd.Value.ToString("yyyy-MM-dd");
    public string? Reason => string.IsNullOrWhiteSpace(_txtReason.Text) ? null : _txtReason.Text.Trim();

    public LeaveRequestForm(List<LeaveTypeDto> leaveTypes)
    {
        Text = "Request Leave";
        Width = 420;
        Height = 310;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;

        var lblType = new Label { Text = "Leave Type:", Left = 20, Top = 20, Width = 100, TextAlign = System.Drawing.ContentAlignment.MiddleRight };
        _cmbLeaveType = new ComboBox { Left = 130, Top = 18, Width = 240, DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbLeaveType.DisplayMember = "Name";
        _cmbLeaveType.DataSource = leaveTypes;

        var lblStart = new Label { Text = "Start Date:", Left = 20, Top = 55, Width = 100, TextAlign = System.Drawing.ContentAlignment.MiddleRight };
        _dtpStart = new DateTimePicker { Left = 130, Top = 53, Width = 240, Format = DateTimePickerFormat.Short, Value = DateTime.Today };

        var lblEnd = new Label { Text = "End Date:", Left = 20, Top = 90, Width = 100, TextAlign = System.Drawing.ContentAlignment.MiddleRight };
        _dtpEnd = new DateTimePicker { Left = 130, Top = 88, Width = 240, Format = DateTimePickerFormat.Short, Value = DateTime.Today };

        _lblTotalDays = new Label { Text = "Total: 1 day(s)", Left = 130, Top = 118, Width = 240, ForeColor = System.Drawing.Color.DimGray };

        var lblReason = new Label { Text = "Reason:", Left = 20, Top = 145, Width = 100, TextAlign = System.Drawing.ContentAlignment.TopRight };
        _txtReason = new TextBox { Left = 130, Top = 143, Width = 240, Height = 55, Multiline = true };

        _dtpStart.ValueChanged += (_, _) => UpdateTotalDays();
        _dtpEnd.ValueChanged += (_, _) => UpdateTotalDays();

        var btnCancel = new Button { Text = "Cancel", Left = 200, Top = 225, Width = 80, Height = 30 };
        var btnSubmit = new Button { Text = "Submit", Left = 292, Top = 225, Width = 80, Height = 30, FlatStyle = FlatStyle.Flat };
        btnSubmit.BackColor = System.Drawing.Color.FromArgb(0, 120, 212);
        btnSubmit.ForeColor = System.Drawing.Color.White;

        btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        btnSubmit.Click += OnSubmit;

        Controls.AddRange(new Control[] { lblType, _cmbLeaveType, lblStart, _dtpStart, lblEnd, _dtpEnd, _lblTotalDays, lblReason, _txtReason, btnCancel, btnSubmit });
    }

    private void UpdateTotalDays()
    {
        if (_dtpEnd.Value < _dtpStart.Value)
            _dtpEnd.Value = _dtpStart.Value;
        var days = (int)(_dtpEnd.Value - _dtpStart.Value).TotalDays + 1;
        _lblTotalDays.Text = $"Total: {days} day(s)";
    }

    private void OnSubmit(object? sender, EventArgs e)
    {
        if (_cmbLeaveType.SelectedItem == null)
        {
            MessageBox.Show("Please select a leave type.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (_dtpEnd.Value < _dtpStart.Value)
        {
            MessageBox.Show("End date cannot be before start date.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        DialogResult = DialogResult.OK;
        Close();
    }
}
