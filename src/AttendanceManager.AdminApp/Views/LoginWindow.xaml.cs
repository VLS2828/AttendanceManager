using System.Windows;

namespace AttendanceManager.AdminApp.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
    }

    private async void BtnLogin_Click(object sender, RoutedEventArgs e)
    {
        TxtError.Visibility = Visibility.Collapsed;
        BtnLogin.IsEnabled = false;
        BtnLogin.Content = "Signing in...";

        try
        {
            var serverUrl = TxtServerUrl.Text.Trim();
            var email = TxtEmail.Text.Trim();
            var password = TxtPassword.Password;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ShowError("Please enter email and password.");
                return;
            }

            App.Api.SetBaseUrl(serverUrl);
            var result = await App.Api.LoginAsync(email, password);

            if (result.Success)
            {
                var mainWindow = new MainWindow();
                mainWindow.Show();
                Close();
            }
            else
            {
                ShowError(result.ErrorMessage ?? "Login failed.");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Connection error: {ex.Message}");
        }
        finally
        {
            BtnLogin.IsEnabled = true;
            BtnLogin.Content = "Sign In";
        }
    }

    private void ShowError(string message)
    {
        TxtError.Text = message;
        TxtError.Visibility = Visibility.Visible;
    }
}
