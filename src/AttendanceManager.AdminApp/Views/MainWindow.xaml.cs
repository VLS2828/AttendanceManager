using System.Windows;
using System.Windows.Controls;
using AttendanceManager.AdminApp.Views.Pages;

namespace AttendanceManager.AdminApp.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        TxtUserName.Text = App.Api.FullName;
        TxtUserRole.Text = App.Api.Role;

        if (App.Api.IsAdmin)
        {
            BtnApprovals.Visibility = Visibility.Visible;
            BtnAuditLog.Visibility = Visibility.Visible;
            BtnSettings.Visibility = Visibility.Visible;
        }

        NavigateTo("Dashboard");
    }

    private void NavButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string page)
        {
            NavigateTo(page);
        }
    }

    private void NavigateTo(string page)
    {
        UserControl? content = page switch
        {
            "Dashboard" => App.Api.IsAdmin ? new AdminDashboardPage() : new EmployeeDashboardPage(),
            "Attendance" => new AttendancePage(),
            "Employees" => new EmployeesPage(),
            "Leaves" => new LeavesPage(),
            "Reports" => new ReportsPage(),
            "Holidays" => new HolidaysPage(),
            "Notifications" => new NotificationsPage(),
            "Approvals" => new ApprovalsPage(),
            "AuditLog" => new AuditLogPage(),
            "Settings" => new SettingsPage(),
            _ => null
        };

        if (content != null)
        {
            ContentArea.Content = content;
        }
    }

    private void BtnLogout_Click(object sender, RoutedEventArgs e)
    {
        var loginWindow = new LoginWindow();
        loginWindow.Show();
        Close();
    }
}
