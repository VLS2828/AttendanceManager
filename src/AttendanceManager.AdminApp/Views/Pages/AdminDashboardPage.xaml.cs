using System.Windows.Controls;

namespace AttendanceManager.AdminApp.Views.Pages;

public partial class AdminDashboardPage : UserControl
{
    public AdminDashboardPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadDashboardAsync();
    }

    private async Task LoadDashboardAsync()
    {
        var dashboard = await App.Api.GetAdminDashboardAsync();
        if (dashboard == null) return;

        TxtTotalEmployees.Text = dashboard.TotalEmployees.ToString();
        TxtOnline.Text = dashboard.EmployeesOnline.ToString();
        TxtPresent.Text = dashboard.PresentToday.ToString();
        TxtAbsent.Text = dashboard.AbsentToday.ToString();
        TxtOnLeave.Text = dashboard.OnLeaveToday.ToString();
        TxtLateArrivals.Text = dashboard.LateArrivals.ToString();
        DgAttendance.ItemsSource = dashboard.TodayAttendance;
    }
}
