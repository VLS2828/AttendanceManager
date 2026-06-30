using System.Windows.Controls;

namespace AttendanceManager.AdminApp.Views.Pages;

public partial class EmployeeDashboardPage : UserControl
{
    public EmployeeDashboardPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadDashboardAsync();
    }

    private async Task LoadDashboardAsync()
    {
        var dashboard = await App.Api.GetEmployeeDashboardAsync(App.Api.EmployeeId);
        if (dashboard == null) return;

        if (dashboard.TodayAttendance != null)
        {
            TxtLoginTime.Text = dashboard.TodayAttendance.LoginTime ?? "--:--";
            TxtLogoutTime.Text = dashboard.TodayAttendance.LogoutTime ?? "--:--";
            TxtTotalHours.Text = dashboard.TodayAttendance.TotalHours.ToString("F2");
            TxtIdleTime.Text = $"{dashboard.TodayAttendance.IdleTimeMinutes:F0} min";
            TxtEffectiveHours.Text = dashboard.TodayAttendance.EffectiveHours.ToString("F2");
        }

        DgLeaveBalance.ItemsSource = dashboard.LeaveBalances;
        DgMonthlyAttendance.ItemsSource = dashboard.MonthlyAttendance;

        TxtPresentDays.Text = dashboard.MonthlySummary.PresentDays.ToString();
        TxtAbsentDays.Text = dashboard.MonthlySummary.AbsentDays.ToString();
        TxtLeaveDays.Text = dashboard.MonthlySummary.LeaveDays.ToString();
        TxtAvgHours.Text = dashboard.MonthlySummary.AverageWorkingHours.ToString("F1");
    }
}
