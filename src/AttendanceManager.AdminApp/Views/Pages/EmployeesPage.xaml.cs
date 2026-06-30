using System.Windows;
using System.Windows.Controls;

namespace AttendanceManager.AdminApp.Views.Pages;

public partial class EmployeesPage : UserControl
{
    public EmployeesPage()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (App.Api.IsAdmin) BtnAdd.Visibility = Visibility.Visible;
            await LoadEmployeesAsync();
        };
    }

    private async Task LoadEmployeesAsync()
    {
        var employees = await App.Api.GetAllEmployeesAsync();
        DgEmployees.ItemsSource = employees;
    }

    private async void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AddEmployeeDialog();
        if (dialog.ShowDialog() == true && dialog.EmployeeData != null)
        {
            var result = await App.Api.CreateEmployeeAsync(dialog.EmployeeData);
            if (result?.Success == true)
            {
                MessageBox.Show("Employee created successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadEmployeesAsync();
            }
            else
            {
                MessageBox.Show("Failed to create employee.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

public class AddEmployeeDialog : Window
{
    public object? EmployeeData { get; private set; }

    public AddEmployeeDialog()
    {
        Title = "Add New Employee";
        Width = 450;
        Height = 520;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;

        var panel = new StackPanel { Margin = new Thickness(20) };

        var txtCode = AddField(panel, "Employee Code:");
        var txtFirstName = AddField(panel, "First Name:");
        var txtLastName = AddField(panel, "Last Name:");
        var txtEmail = AddField(panel, "Email:");
        var txtPassword = AddField(panel, "Password:");
        var txtPhone = AddField(panel, "Phone:");
        var txtDeptId = AddField(panel, "Department ID:");
        var cmbRole = new ComboBox { Margin = new Thickness(0, 2, 0, 8) };
        cmbRole.Items.Add("Employee"); cmbRole.Items.Add("Admin");
        cmbRole.SelectedIndex = 0;
        panel.Children.Add(new TextBlock { Text = "Role:" });
        panel.Children.Add(cmbRole);

        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 15, 0, 0) };
        var okBtn = new Button { Content = "Create", Width = 80, Margin = new Thickness(0, 0, 10, 0) };
        okBtn.Click += (_, _) =>
        {
            EmployeeData = new
            {
                EmployeeCode = txtCode.Text,
                FirstName = txtFirstName.Text,
                LastName = txtLastName.Text,
                Email = txtEmail.Text,
                Password = txtPassword.Text,
                Phone = txtPhone.Text,
                DepartmentId = int.TryParse(txtDeptId.Text, out var d) ? d : 1,
                Role = cmbRole.SelectedItem?.ToString() ?? "Employee",
                JoiningDate = DateTime.Today.ToString("yyyy-MM-dd")
            };
            DialogResult = true;
            Close();
        };
        var cancelBtn = new Button { Content = "Cancel", Width = 80, IsCancel = true };
        btnPanel.Children.Add(okBtn);
        btnPanel.Children.Add(cancelBtn);
        panel.Children.Add(btnPanel);

        Content = new ScrollViewer { Content = panel };
    }

    private static TextBox AddField(StackPanel panel, string label)
    {
        panel.Children.Add(new TextBlock { Text = label });
        var txt = new TextBox { Margin = new Thickness(0, 2, 0, 8) };
        panel.Children.Add(txt);
        return txt;
    }
}
