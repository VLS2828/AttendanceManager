using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace AttendanceManager.Agent.Services;

public static class StartupManager
{
    private const string AppName = "AttendanceManagerAgent";
    private const string RegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    public static void EnableAutoStart(ILogger logger)
    {
        try
        {
            if (!OperatingSystem.IsWindows()) return;

            var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath)) return;

            using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, true);
            key?.SetValue(AppName, $"\"{exePath}\"");
            logger.LogInformation("Auto-start enabled for {Path}", exePath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to enable auto-start");
        }
    }

    public static void DisableAutoStart(ILogger logger)
    {
        try
        {
            if (!OperatingSystem.IsWindows()) return;

            using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, true);
            key?.DeleteValue(AppName, false);
            logger.LogInformation("Auto-start disabled");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to disable auto-start");
        }
    }

    public static bool IsAutoStartEnabled()
    {
        try
        {
            if (!OperatingSystem.IsWindows()) return false;

            using var key = Registry.CurrentUser.OpenSubKey(RegistryPath);
            return key?.GetValue(AppName) != null;
        }
        catch
        {
            return false;
        }
    }
}
