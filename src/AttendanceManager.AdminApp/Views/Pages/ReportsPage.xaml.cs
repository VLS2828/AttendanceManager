using System.Collections;
using System.Windows;
using System.Windows.Controls;
using AttendanceManager.Shared.DTOs;

namespace AttendanceManager.AdminApp.Views.Pages;

public partial class ReportsPage : UserControl
{
    private IList? _currentData;

    public ReportsPage()
    {
        InitializeComponent();
    }

    private async void BtnGenerate_Click(object sender, RoutedEventArgs e)
    {
        var selectedReport = (CmbReport.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "daily";
        var startDate = DpStart.SelectedDate?.ToString("yyyy-MM-dd") ?? DateTime.Today.ToString("yyyy-MM-dd");
        var endDate = DpEnd.SelectedDate?.ToString("yyyy-MM-dd") ?? DateTime.Today.ToString("yyyy-MM-dd");

        TxtReportTitle.Text = (CmbReport.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Report";
        DgReport.AutoGenerateColumns = true;

        var parameters = new Dictionary<string, string>
        {
            { "startDate", startDate },
            { "endDate", endDate }
        };

        var data = await App.Api.GetReportAsync(selectedReport, parameters);
        _currentData = data;
        DgReport.ItemsSource = data;
    }

    private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
    {
        if (_currentData == null || _currentData.Count == 0)
        {
            MessageBox.Show("No data to export. Generate a report first.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Excel Files|*.xlsx",
            FileName = $"Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                ExportToExcel(dialog.FileName);
                MessageBox.Show("Report exported successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void BtnDownloadCsv_Click(object sender, RoutedEventArgs e)
    {
        var startDate = DpStart.SelectedDate?.ToString("yyyy-MM-dd") ?? DateTime.Today.ToString("yyyy-MM-dd");
        var endDate = DpEnd.SelectedDate?.ToString("yyyy-MM-dd") ?? DateTime.Today.ToString("yyyy-MM-dd");

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "CSV Files|*.csv",
            FileName = $"attendance_{startDate}_to_{endDate}.csv"
        };

        if (dialog.ShowDialog() != true) return;

        var bytes = await App.Api.DownloadAttendanceCsvAsync(startDate, endDate);
        if (bytes == null || bytes.Length == 0)
        {
            MessageBox.Show("No data returned from server.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        System.IO.File.WriteAllBytes(dialog.FileName, bytes);
        MessageBox.Show("CSV downloaded successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ExportToExcel(string filePath)
    {
        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Report");

        if (_currentData is List<AttendanceDto> attendanceList)
        {
            worksheet.Cell(1, 1).Value = "Employee";
            worksheet.Cell(1, 2).Value = "Date";
            worksheet.Cell(1, 3).Value = "Login Time";
            worksheet.Cell(1, 4).Value = "Logout Time";
            worksheet.Cell(1, 5).Value = "Status";
            worksheet.Cell(1, 6).Value = "Total Hours";
            worksheet.Cell(1, 7).Value = "Idle Time (min)";
            worksheet.Cell(1, 8).Value = "Effective Hours";

            for (int i = 0; i < attendanceList.Count; i++)
            {
                var item = attendanceList[i];
                worksheet.Cell(i + 2, 1).Value = item.EmployeeName;
                worksheet.Cell(i + 2, 2).Value = item.Date;
                worksheet.Cell(i + 2, 3).Value = item.LoginTime ?? "";
                worksheet.Cell(i + 2, 4).Value = item.LogoutTime ?? "";
                worksheet.Cell(i + 2, 5).Value = item.Status;
                worksheet.Cell(i + 2, 6).Value = item.TotalHours;
                worksheet.Cell(i + 2, 7).Value = item.IdleTimeMinutes;
                worksheet.Cell(i + 2, 8).Value = item.EffectiveHours;
            }

            var range = worksheet.Range(1, 1, attendanceList.Count + 1, 8);
            range.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
            range.Style.Border.InsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;

            var headerRange = worksheet.Range(1, 1, 1, 8);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightBlue;
        }

        worksheet.Columns().AdjustToContents();
        workbook.SaveAs(filePath);
    }
}
