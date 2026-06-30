using System.Windows;
using System.Windows.Controls;

namespace AttendanceManager.AdminApp.Views.Pages;

public partial class HolidaysPage : UserControl
{
    public HolidaysPage()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (App.Api.IsAdmin) BtnAdd.Visibility = Visibility.Visible;
            await LoadHolidaysAsync();
        };
    }

    private async Task LoadHolidaysAsync()
    {
        var holidays = await App.Api.GetHolidaysAsync();
        DgHolidays.ItemsSource = holidays;
    }

    private async void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AddHolidayDialog();
        if (dialog.ShowDialog() == true)
        {
            // holiday is added via dialog
            await LoadHolidaysAsync();
        }
    }
}

public class AddHolidayDialog : Window
{
    public AddHolidayDialog()
    {
        Title = "Add Holiday";
        Width = 400;
        Height = 280;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;

        var panel = new StackPanel { Margin = new Thickness(20) };

        panel.Children.Add(new TextBlock { Text = "Holiday Name:" });
        var txtName = new TextBox { Margin = new Thickness(0, 2, 0, 8) };
        panel.Children.Add(txtName);

        panel.Children.Add(new TextBlock { Text = "Date:" });
        var dpDate = new DatePicker { Margin = new Thickness(0, 2, 0, 8), SelectedDate = DateTime.Today };
        panel.Children.Add(dpDate);

        panel.Children.Add(new TextBlock { Text = "Description:" });
        var txtDesc = new TextBox { Margin = new Thickness(0, 2, 0, 8) };
        panel.Children.Add(txtDesc);

        var chkOptional = new CheckBox { Content = "Optional Holiday", Margin = new Thickness(0, 0, 0, 15) };
        panel.Children.Add(chkOptional);

        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var okBtn = new Button { Content = "Add", Width = 80, Margin = new Thickness(0, 0, 10, 0) };
        okBtn.Click += async (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(txtName.Text) || dpDate.SelectedDate == null)
            {
                MessageBox.Show("Please enter holiday name and date.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = await App.Api.CreateHolidayAsync(new
            {
                Name = txtName.Text.Trim(),
                Date = dpDate.SelectedDate.Value.ToString("yyyy-MM-dd"),
                Description = txtDesc.Text.Trim(),
                IsOptional = chkOptional.IsChecked == true
            });

            if (result?.Success == true)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show(result?.Message ?? "Failed to add holiday.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };
        var cancelBtn = new Button { Content = "Cancel", Width = 80, IsCancel = true };
        btnPanel.Children.Add(okBtn);
        btnPanel.Children.Add(cancelBtn);
        panel.Children.Add(btnPanel);

        Content = panel;
    }
}
