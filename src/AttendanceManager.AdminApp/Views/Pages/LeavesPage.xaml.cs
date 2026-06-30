using System.Windows;
using System.Windows.Controls;
using AttendanceManager.Shared.DTOs;

namespace AttendanceManager.AdminApp.Views.Pages;

public partial class LeavesPage : UserControl
{
    public LeavesPage()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (App.Api.IsAdmin)
            {
                PnlPending.Visibility = Visibility.Visible;
                TxtLeaveTitle.Text = "All Leave Requests";
            }
            await LoadDataAsync();
        };
    }

    private async Task LoadDataAsync()
    {
        if (App.Api.IsAdmin)
        {
            var pending = await App.Api.GetPendingLeavesAsync();
            DgPending.ItemsSource = pending;
        }

        var myLeaves = await App.Api.GetEmployeeLeavesAsync(App.Api.EmployeeId);
        DgMyLeaves.ItemsSource = myLeaves;
    }

    private void DgPending_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        PnlPendingActions.Visibility = DgPending.SelectedItem != null ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void BtnApprove_Click(object sender, RoutedEventArgs e)
    {
        if (DgPending.SelectedItem is not LeaveRequestDto selected) return;
        var dialog = new InputDialog("Remarks", "Enter approval remarks (optional):", "");
        if (dialog.ShowDialog() == true)
        {
            await App.Api.ApproveLeaveAsync(selected.Id, dialog.Value);
            await LoadDataAsync();
        }
    }

    private async void BtnReject_Click(object sender, RoutedEventArgs e)
    {
        if (DgPending.SelectedItem is not LeaveRequestDto selected) return;
        var dialog = new InputDialog("Remarks", "Enter rejection reason:", "");
        if (dialog.ShowDialog() == true)
        {
            await App.Api.RejectLeaveAsync(selected.Id, dialog.Value);
            await LoadDataAsync();
        }
    }

    private async void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        if (DgPending.SelectedItem is not LeaveRequestDto selected) return;
        var result = MessageBox.Show("Cancel this leave request?", "Confirm", MessageBoxButton.YesNo);
        if (result == MessageBoxResult.Yes)
        {
            await App.Api.CancelLeaveAsync(selected.Id, "Cancelled by admin");
            await LoadDataAsync();
        }
    }

    private async void BtnApply_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ApplyLeaveDialog();
        if (dialog.ShowDialog() == true && dialog.LeaveData != null)
        {
            await App.Api.SubmitLeaveRequestAsync(dialog.LeaveData);
            await LoadDataAsync();
        }
    }
}

public class ApplyLeaveDialog : Window
{
    public object? LeaveData { get; private set; }

    public ApplyLeaveDialog()
    {
        Title = "Apply for Leave";
        Width = 400;
        Height = 350;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;

        var panel = new StackPanel { Margin = new Thickness(20) };

        panel.Children.Add(new TextBlock { Text = "Leave Type:" });
        var cmbType = new ComboBox { Margin = new Thickness(0, 2, 0, 8) };
        cmbType.Items.Add(new ComboBoxItem { Content = "Casual Leave", Tag = 1 });
        cmbType.Items.Add(new ComboBoxItem { Content = "Sick Leave", Tag = 2 });
        cmbType.Items.Add(new ComboBoxItem { Content = "Earned Leave", Tag = 3 });
        cmbType.Items.Add(new ComboBoxItem { Content = "Compensatory Off", Tag = 4 });
        cmbType.SelectedIndex = 0;
        panel.Children.Add(cmbType);

        panel.Children.Add(new TextBlock { Text = "Start Date:" });
        var dpStart = new DatePicker { Margin = new Thickness(0, 2, 0, 8), SelectedDate = DateTime.Today };
        panel.Children.Add(dpStart);

        panel.Children.Add(new TextBlock { Text = "End Date:" });
        var dpEnd = new DatePicker { Margin = new Thickness(0, 2, 0, 8), SelectedDate = DateTime.Today };
        panel.Children.Add(dpEnd);

        panel.Children.Add(new TextBlock { Text = "Reason:" });
        var txtReason = new TextBox { Margin = new Thickness(0, 2, 0, 15), Height = 60, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true };
        panel.Children.Add(txtReason);

        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var okBtn = new Button { Content = "Submit", Width = 80, Margin = new Thickness(0, 0, 10, 0) };
        okBtn.Click += (_, _) =>
        {
            var selectedType = cmbType.SelectedItem as ComboBoxItem;
            LeaveData = new
            {
                EmployeeId = App.Api.EmployeeId,
                LeaveTypeId = (int)(selectedType?.Tag ?? 1),
                StartDate = dpStart.SelectedDate?.ToString("yyyy-MM-dd") ?? "",
                EndDate = dpEnd.SelectedDate?.ToString("yyyy-MM-dd") ?? "",
                Reason = txtReason.Text
            };
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
