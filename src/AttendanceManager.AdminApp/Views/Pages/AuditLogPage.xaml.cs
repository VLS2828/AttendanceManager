using System.Windows;
using System.Windows.Controls;

namespace AttendanceManager.AdminApp.Views.Pages;

public partial class AuditLogPage : UserControl
{
    public AuditLogPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAuditLogsAsync();
    }

    private async Task LoadAuditLogsAsync()
    {
        var startDate = DpStart.SelectedDate?.ToString("yyyy-MM-dd");
        var endDate = DpEnd.SelectedDate?.ToString("yyyy-MM-dd");
        var logs = await App.Api.GetAuditLogsAsync(null, startDate, endDate);
        DgAuditLogs.ItemsSource = logs;
    }

    private async void BtnSearch_Click(object sender, RoutedEventArgs e) => await LoadAuditLogsAsync();
}
