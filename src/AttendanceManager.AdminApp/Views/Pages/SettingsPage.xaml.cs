using System.Windows.Controls;

namespace AttendanceManager.AdminApp.Views.Pages;

public partial class SettingsPage : UserControl
{
    public SettingsPage()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            // Settings data would be loaded from API
            // For now, display a placeholder
        };
    }
}
