using System.Windows;
using System.Windows.Controls;

namespace AttendanceManager.AdminApp.Views.Pages;

public partial class NotificationsPage : UserControl
{
    public NotificationsPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadNotificationsAsync();
    }

    private async Task LoadNotificationsAsync()
    {
        var notifications = await App.Api.GetNotificationsAsync(App.Api.EmployeeId);
        DgNotifications.ItemsSource = notifications;
    }

    private async void BtnMarkAllRead_Click(object sender, RoutedEventArgs e)
    {
        await App.Api.MarkNotificationAsReadAsync(App.Api.EmployeeId);
        await LoadNotificationsAsync();
    }
}
