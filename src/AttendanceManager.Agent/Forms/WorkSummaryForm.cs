using AttendanceManager.Shared.DTOs;

namespace AttendanceManager.Agent.Forms;

internal class WorkSummaryForm : Form
{
    private readonly DataGridView _grid;
    public List<WorkSummaryEntryDto> Entries { get; private set; } = new();

    public WorkSummaryForm()
    {
        Text = "Daily Work Summary";
        Width = 680;
        Height = 380;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;

        var label = new Label
        {
            Text = "Enter your work summary for today. At least one entry is required before clocking out.",
            Left = 12, Top = 12, Width = 640, Height = 32,
            ForeColor = System.Drawing.Color.DimGray
        };

        _grid = new DataGridView
        {
            Left = 12, Top = 52, Width = 640, Height = 220,
            AllowUserToAddRows = true,
            AllowUserToDeleteRows = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect
        };
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "From", HeaderText = "From (HH:mm)", FillWeight = 15 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "To", HeaderText = "To (HH:mm)", FillWeight = 15 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Client", HeaderText = "Client", FillWeight = 25 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Description", HeaderText = "Description", FillWeight = 45 });

        var btnCancel = new Button { Text = "Cancel", Left = 440, Top = 295, Width = 90, Height = 32 };
        var btnSubmit = new Button { Text = "Submit && Clock Out", Left = 542, Top = 295, Width = 130, Height = 32, FlatStyle = FlatStyle.Flat };
        btnSubmit.BackColor = System.Drawing.Color.FromArgb(0, 120, 212);
        btnSubmit.ForeColor = System.Drawing.Color.White;

        btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        btnSubmit.Click += OnSubmit;

        Controls.AddRange(new Control[] { label, _grid, btnCancel, btnSubmit });
    }

    private void OnSubmit(object? sender, EventArgs e)
    {
        var entries = new List<WorkSummaryEntryDto>();
        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.IsNewRow) continue;
            var from = row.Cells["From"].Value?.ToString()?.Trim();
            var to = row.Cells["To"].Value?.ToString()?.Trim();
            var client = row.Cells["Client"].Value?.ToString()?.Trim();
            var desc = row.Cells["Description"].Value?.ToString()?.Trim();

            if (string.IsNullOrEmpty(from) && string.IsNullOrEmpty(to) && string.IsNullOrEmpty(client) && string.IsNullOrEmpty(desc))
                continue;

            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to) || string.IsNullOrEmpty(client) || string.IsNullOrEmpty(desc))
            {
                MessageBox.Show("Please fill in all fields for each row.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            entries.Add(new WorkSummaryEntryDto { FromTime = from, ToTime = to, Client = client, Description = desc });
        }

        if (entries.Count == 0)
        {
            MessageBox.Show("Please add at least one work entry.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Entries = entries;
        DialogResult = DialogResult.OK;
        Close();
    }
}
