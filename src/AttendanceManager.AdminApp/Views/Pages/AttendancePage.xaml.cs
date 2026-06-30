using System.Windows;
using System.Windows.Controls;
using AttendanceManager.Shared.DTOs;

namespace AttendanceManager.AdminApp.Views.Pages;

public partial class AttendancePage : UserControl
{
    public AttendancePage()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (App.Api.IsAdmin)
            {
                BtnManualEntry.Visibility = Visibility.Visible;
                PnlActions.Visibility = Visibility.Visible;
            }
            await LoadAttendanceAsync();
        };
    }

    private async Task LoadAttendanceAsync()
    {
        var date = DpDate.SelectedDate?.ToString("yyyy-MM-dd") ?? DateTime.Today.ToString("yyyy-MM-dd");

        if (App.Api.IsAdmin)
        {
            var data = await App.Api.GetDailyAttendanceAsync(date);
            DgAttendance.ItemsSource = data;
        }
        else
        {
            var data = await App.Api.GetAttendanceRangeAsync(App.Api.EmployeeId, date, date);
            DgAttendance.ItemsSource = data;
        }
    }

    private async void BtnLoad_Click(object sender, RoutedEventArgs e) => await LoadAttendanceAsync();

    private void DgAttendance_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        PnlActions.Visibility = DgAttendance.SelectedItem != null && App.Api.IsAdmin
            ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void BtnEditStatus_Click(object sender, RoutedEventArgs e)
    {
        if (DgAttendance.SelectedItem is not AttendanceDto selected) return;

        var dialog = new InputDialog("Edit Status", "Enter new status (Present, Absent, Leave, HalfDay):", selected.Status);
        if (dialog.ShowDialog() == true)
        {
            var reasonDialog = new InputDialog("Reason", "Enter reason for modification:", "");
            if (reasonDialog.ShowDialog() == true)
            {
                await App.Api.UpdateAttendanceStatusAsync(selected.Id, dialog.Value, reasonDialog.Value);
                await LoadAttendanceAsync();
            }
        }
    }

    private async void BtnEditLoginTime_Click(object sender, RoutedEventArgs e)
    {
        if (DgAttendance.SelectedItem is not AttendanceDto selected) return;

        var dialog = new InputDialog("Edit Login Time", "Enter new login time (yyyy-MM-dd HH:mm:ss):", selected.LoginTime ?? "");
        if (dialog.ShowDialog() == true)
        {
            var reasonDialog = new InputDialog("Reason", "Enter reason:", "");
            if (reasonDialog.ShowDialog() == true)
            {
                await App.Api.UpdateLoginTimeAsync(selected.Id, dialog.Value, reasonDialog.Value);
                await LoadAttendanceAsync();
            }
        }
    }

    private async void BtnEditLogoutTime_Click(object sender, RoutedEventArgs e)
    {
        if (DgAttendance.SelectedItem is not AttendanceDto selected) return;

        var dialog = new InputDialog("Edit Logout Time", "Enter new logout time (yyyy-MM-dd HH:mm:ss):", selected.LogoutTime ?? "");
        if (dialog.ShowDialog() == true)
        {
            var reasonDialog = new InputDialog("Reason", "Enter reason:", "");
            if (reasonDialog.ShowDialog() == true)
            {
                await App.Api.UpdateLogoutTimeAsync(selected.Id, dialog.Value, reasonDialog.Value);
                await LoadAttendanceAsync();
            }
        }
    }

    private async void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (DgAttendance.SelectedItem is not AttendanceDto selected) return;

        var result = MessageBox.Show("Are you sure you want to delete this attendance record?",
            "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            var reasonDialog = new InputDialog("Reason", "Enter reason for deletion:", "");
            if (reasonDialog.ShowDialog() == true)
            {
                await App.Api.DeleteAttendanceAsync(selected.Id, reasonDialog.Value);
                await LoadAttendanceAsync();
            }
        }
    }

    private async void BtnManualEntry_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ManualAttendanceDialog();
        if (dialog.ShowDialog() == true && dialog.AttendanceData != null)
        {
            await App.Api.InsertManualAttendanceAsync(dialog.AttendanceData, dialog.Reason);
            await LoadAttendanceAsync();
        }
    }
}

public class InputDialog : Window
{
    private readonly TextBox _textBox;
    public string Value => _textBox.Text;

    public InputDialog(string title, string prompt, string defaultValue)
    {
        Title = title;
        Width = 400;
        Height = 180;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;

        var panel = new StackPanel { Margin = new Thickness(20) };
        panel.Children.Add(new TextBlock { Text = prompt, Margin = new Thickness(0, 0, 0, 10) });
        _textBox = new TextBox { Text = defaultValue, Margin = new Thickness(0, 0, 0, 15) };
        panel.Children.Add(_textBox);

        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var okBtn = new Button { Content = "OK", Width = 80, Margin = new Thickness(0, 0, 10, 0), IsDefault = true };
        okBtn.Click += (_, _) => { DialogResult = true; Close(); };
        var cancelBtn = new Button { Content = "Cancel", Width = 80, IsCancel = true };
        cancelBtn.Click += (_, _) => { DialogResult = false; Close(); };
        btnPanel.Children.Add(okBtn);
        btnPanel.Children.Add(cancelBtn);
        panel.Children.Add(btnPanel);

        Content = panel;
    }
}

public class ManualAttendanceDialog : Window
{
    public AttendanceDto? AttendanceData { get; private set; }
    public string Reason { get; private set; } = "";

    private readonly TextBox _txtEmployeeId;
    private readonly TextBox _txtDate;
    private readonly TextBox _txtLoginTime;
    private readonly TextBox _txtLogoutTime;
    private readonly ComboBox _cmbStatus;
    private readonly TextBox _txtRemarks;
    private readonly TextBox _txtReason;

    public ManualAttendanceDialog()
    {
        Title = "Insert Manual Attendance";
        Width = 450;
        Height = 420;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;

        var panel = new StackPanel { Margin = new Thickness(20) };

        panel.Children.Add(new TextBlock { Text = "Employee ID:" });
        _txtEmployeeId = new TextBox { Margin = new Thickness(0, 2, 0, 8) };
        panel.Children.Add(_txtEmployeeId);

        panel.Children.Add(new TextBlock { Text = "Date (yyyy-MM-dd):" });
        _txtDate = new TextBox { Text = DateTime.Today.ToString("yyyy-MM-dd"), Margin = new Thickness(0, 2, 0, 8) };
        panel.Children.Add(_txtDate);

        panel.Children.Add(new TextBlock { Text = "Login Time (yyyy-MM-dd HH:mm:ss):" });
        _txtLoginTime = new TextBox { Margin = new Thickness(0, 2, 0, 8) };
        panel.Children.Add(_txtLoginTime);

        panel.Children.Add(new TextBlock { Text = "Logout Time (yyyy-MM-dd HH:mm:ss):" });
        _txtLogoutTime = new TextBox { Margin = new Thickness(0, 2, 0, 8) };
        panel.Children.Add(_txtLogoutTime);

        panel.Children.Add(new TextBlock { Text = "Status:" });
        _cmbStatus = new ComboBox { Margin = new Thickness(0, 2, 0, 8) };
        _cmbStatus.Items.Add("Present"); _cmbStatus.Items.Add("Absent"); _cmbStatus.Items.Add("Leave");
        _cmbStatus.Items.Add("HalfDay"); _cmbStatus.Items.Add("WorkFromHome");
        _cmbStatus.SelectedIndex = 0;
        panel.Children.Add(_cmbStatus);

        panel.Children.Add(new TextBlock { Text = "Remarks:" });
        _txtRemarks = new TextBox { Margin = new Thickness(0, 2, 0, 8) };
        panel.Children.Add(_txtRemarks);

        panel.Children.Add(new TextBlock { Text = "Reason for manual entry:" });
        _txtReason = new TextBox { Margin = new Thickness(0, 2, 0, 15) };
        panel.Children.Add(_txtReason);

        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var okBtn = new Button { Content = "Insert", Width = 80, Margin = new Thickness(0, 0, 10, 0) };
        okBtn.Click += (_, _) =>
        {
            AttendanceData = new AttendanceDto
            {
                EmployeeId = int.TryParse(_txtEmployeeId.Text, out var id) ? id : 0,
                Date = _txtDate.Text,
                LoginTime = string.IsNullOrWhiteSpace(_txtLoginTime.Text) ? null : _txtLoginTime.Text,
                LogoutTime = string.IsNullOrWhiteSpace(_txtLogoutTime.Text) ? null : _txtLogoutTime.Text,
                Status = _cmbStatus.SelectedItem?.ToString() ?? "Present",
                Remarks = _txtRemarks.Text
            };
            Reason = _txtReason.Text;
            DialogResult = true;
            Close();
        };
        var cancelBtn = new Button { Content = "Cancel", Width = 80, IsCancel = true };
        btnPanel.Children.Add(okBtn);
        btnPanel.Children.Add(cancelBtn);
        panel.Children.Add(btnPanel);

        Content = panel;
    }
}
