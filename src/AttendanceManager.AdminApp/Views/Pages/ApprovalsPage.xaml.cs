using System.Windows;
using System.Windows.Controls;
using AttendanceManager.Shared.DTOs;

namespace AttendanceManager.AdminApp.Views.Pages;

public partial class ApprovalsPage : UserControl
{
    public ApprovalsPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var items = await App.Api.GetPendingApprovalsAsync();
        DgApprovals.ItemsSource = items;
        TxtCount.Text = items.Count == 0
            ? "No pending approvals."
            : $"{items.Count} item(s) awaiting approval.";
        PnlActions.Visibility = Visibility.Collapsed;
    }

    private void DgApprovals_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        PnlActions.Visibility = DgApprovals.SelectedItem != null ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void BtnRefresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();

    private async void BtnApprove_Click(object sender, RoutedEventArgs e)
    {
        if (DgApprovals.SelectedItem is not ApprovalItemDto item) return;

        var dialog = new InputDialog("Approve", "Comment (optional):", "");
        if (dialog.ShowDialog() != true) return;

        ApiResponse? result = item.Type == "Leave"
            ? await App.Api.ApproveLeaveAsync(item.Id, dialog.Value)
            : await App.Api.ApproveCorrectionAsync(item.Id, dialog.Value);

        if (result?.Success == true)
            await LoadAsync();
        else
            MessageBox.Show(result?.Message ?? "Failed to approve.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private async void BtnReject_Click(object sender, RoutedEventArgs e)
    {
        if (DgApprovals.SelectedItem is not ApprovalItemDto item) return;

        var dialog = new InputDialog("Reject", "Reason for rejection:", "");
        if (dialog.ShowDialog() != true) return;

        ApiResponse? result = item.Type == "Leave"
            ? await App.Api.RejectLeaveAsync(item.Id, dialog.Value)
            : await App.Api.RejectCorrectionAsync(item.Id, dialog.Value);

        if (result?.Success == true)
            await LoadAsync();
        else
            MessageBox.Show(result?.Message ?? "Failed to reject.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
