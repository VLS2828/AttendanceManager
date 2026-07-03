using System.Text.Json;
using AttendanceManager.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace AttendanceManager.Agent.Services;

public class AttendanceAgent
{
    private readonly ApiClient _apiClient;
    private readonly IdleDetector _idleDetector;
    private readonly ILogger<AttendanceAgent> _logger;
    private readonly string _configPath;
    private AgentConfig? _config;
    private bool _loginRecordedToday;
    private DateOnly _lastLoginDate;
    private readonly System.Timers.Timer _syncTimer;
    private readonly System.Timers.Timer _notificationTimer;
    private readonly List<PendingIdleRecord> _pendingIdleRecords = new();

    public event EventHandler<string>? StatusChanged;
    public event EventHandler<List<NotificationDto>>? NotificationsReceived;

    public bool IsAuthenticated => _config?.EmployeeId > 0;
    public string EmployeeName => _config?.EmployeeName ?? "Not Configured";

    public AttendanceAgent(ApiClient apiClient, IdleDetector idleDetector, ILogger<AttendanceAgent> logger)
    {
        _apiClient = apiClient;
        _idleDetector = idleDetector;
        _logger = logger;
        _configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AttendanceManager", "agent_config.json");

        _syncTimer = new System.Timers.Timer(300_000); // sync every 5 minutes
        _syncTimer.Elapsed += async (_, _) => await SyncPendingDataAsync();
        _syncTimer.AutoReset = true;

        _notificationTimer = new System.Timers.Timer(60_000); // check every minute
        _notificationTimer.Elapsed += async (_, _) => await CheckNotificationsAsync();
        _notificationTimer.AutoReset = true;

        _idleDetector.IdlePeriodDetected += OnIdlePeriodDetected;
    }

    public async Task InitializeAsync()
    {
        LoadConfig();

        if (IsAuthenticated)
        {
            await RecordLoginAsync();
            _idleDetector.Start();
            _syncTimer.Start();
            _notificationTimer.Start();
            StatusChanged?.Invoke(this, "Online - Tracking Active");
        }
        else
        {
            StatusChanged?.Invoke(this, "Not Configured - Please set up credentials");
        }
    }

    public async Task<bool> ConfigureAsync(string email, string password, string serverUrl)
    {
        var loginResponse = await _apiClient.AuthenticateAsync(new LoginRequest
        {
            Email = email,
            Password = password
        });

        if (loginResponse == null || !loginResponse.Success)
        {
            _logger.LogWarning("Authentication failed for {Email}", email);
            return false;
        }

        return await ApplyLoginResponse(loginResponse, email, serverUrl);
    }

    public async Task<bool> ConfigureByPinAsync(string email, string pin, string serverUrl)
    {
        var loginResponse = await _apiClient.AuthenticateByPinAsync(new LoginPinRequest
        {
            Email = email,
            Pin = pin
        });

        if (loginResponse == null || !loginResponse.Success)
        {
            _logger.LogWarning("PIN authentication failed for {Email}", email);
            return false;
        }

        return await ApplyLoginResponse(loginResponse, email, serverUrl);
    }

    private async Task<bool> ApplyLoginResponse(LoginResponse loginResponse, string email, string serverUrl)
    {
        _config = new AgentConfig
        {
            EmployeeId = loginResponse.EmployeeId,
            EmployeeName = loginResponse.FullName,
            Email = email,
            Token = loginResponse.Token ?? "",
            ServerUrl = serverUrl
        };

        SaveConfig();
        await InitializeAsync();
        return true;
    }

    public async Task RecordLoginAsync()
    {
        if (_config == null || _config.EmployeeId <= 0) return;

        var today = DateOnly.FromDateTime(DateTime.Now);
        if (_loginRecordedToday && _lastLoginDate == today) return;

        var request = new AgentLoginRequest
        {
            EmployeeId = _config.EmployeeId,
            ComputerName = Environment.MachineName,
            WindowsUsername = Environment.UserName,
            IpAddress = GetLocalIpAddress()
        };

        var result = await _apiClient.RecordLoginAsync(request);
        if (result?.Success == true)
        {
            _loginRecordedToday = true;
            _lastLoginDate = today;
            StatusChanged?.Invoke(this, "Online - Login Recorded");
            _logger.LogInformation("Login recorded for Employee {Id}", _config.EmployeeId);
        }
        else
        {
            StatusChanged?.Invoke(this, "Online - Login Pending (will retry)");
            _logger.LogWarning("Failed to record login, will retry on next sync");
        }
    }

    public async Task RecordLogoutAsync()
    {
        if (_config == null || _config.EmployeeId <= 0) return;

        // send any pending idle records first
        await SyncPendingDataAsync();

        var result = await _apiClient.RecordLogoutAsync(new AgentLogoutRequest
        {
            EmployeeId = _config.EmployeeId
        });

        if (result?.Success == true)
        {
            _logger.LogInformation("Logout recorded for Employee {Id}", _config.EmployeeId);
        }
        else
        {
            _logger.LogWarning("Failed to record logout");
        }

        _idleDetector.Stop();
        _syncTimer.Stop();
        _notificationTimer.Stop();
    }

    public async Task<(bool Success, string? Error)> TryClockOutAsync()
    {
        if (_config == null || _config.EmployeeId <= 0)
            return (false, "Not configured.");

        await SyncPendingDataAsync();

        var result = await _apiClient.RecordLogoutAsync(new AgentLogoutRequest { EmployeeId = _config.EmployeeId });
        if (result?.Success == true)
        {
            _idleDetector.Stop();
            _syncTimer.Stop();
            _notificationTimer.Stop();
            StatusChanged?.Invoke(this, "Clocked Out");
            _logger.LogInformation("Manual clock-out for Employee {Id}", _config.EmployeeId);
            return (true, null);
        }

        return (false, result?.Message ?? "Failed to clock out.");
    }

    public async Task<(bool Success, string? Error)> SubmitWorkSummaryAsync(List<WorkSummaryEntryDto> entries)
    {
        if (_config == null) return (false, "Not configured.");

        var request = new SubmitWorkSummaryRequest
        {
            EmployeeId = _config.EmployeeId,
            Date = DateOnly.FromDateTime(DateTime.Now).ToString("yyyy-MM-dd"),
            Entries = entries
        };

        var result = await _apiClient.SubmitWorkSummaryAsync(request, _config.Token);
        if (result?.Success == true) return (true, null);
        return (false, result?.Message ?? "Failed to submit work summary.");
    }

    public async Task<(bool Success, string? Error)> SubmitLeaveRequestAsync(int leaveTypeId, string startDate, string endDate, string? reason)
    {
        if (_config == null) return (false, "Not configured.");

        var request = new SubmitLeaveRequest
        {
            EmployeeId = _config.EmployeeId,
            LeaveTypeId = leaveTypeId,
            StartDate = startDate,
            EndDate = endDate,
            Reason = reason
        };

        var result = await _apiClient.SubmitLeaveRequestAsync(request, _config.Token);
        if (result?.Success == true) return (true, null);
        return (false, result?.Message ?? "Failed to submit leave request.");
    }

    public async Task<(bool Success, string? Error)> SubmitCorrectionAsync(string date, string claimedStatus, string? loginTime, string? logoutTime, string reason)
    {
        if (_config == null) return (false, "Not configured.");

        var request = new SubmitCorrectionRequest
        {
            EmployeeId = _config.EmployeeId,
            Date = date,
            ClaimedStatus = claimedStatus,
            ClaimedLoginTime = loginTime,
            ClaimedLogoutTime = logoutTime,
            Reason = reason
        };

        var result = await _apiClient.SubmitCorrectionAsync(request, _config.Token);
        if (result?.Success == true) return (true, null);
        return (false, result?.Message ?? "Failed to submit correction.");
    }

    public async Task<List<LeaveTypeDto>> GetLeaveTypesAsync()
    {
        if (_config == null) return new();
        return await _apiClient.GetLeaveTypesAsync(_config.Token) ?? new();
    }

    public void Shutdown()
    {
        _idleDetector.Stop();
        _syncTimer.Stop();
        _notificationTimer.Stop();
    }

    private async void OnIdlePeriodDetected(object? sender, IdleEventArgs e)
    {
        if (_config == null) return;

        var idleRecord = new IdleTimeDto
        {
            EmployeeId = _config.EmployeeId,
            IdleStartTime = e.StartTime,
            IdleEndTime = e.EndTime,
            DurationMinutes = e.DurationMinutes
        };

        var success = await _apiClient.RecordIdleTimeAsync(idleRecord);
        if (!success)
        {
            _pendingIdleRecords.Add(new PendingIdleRecord
            {
                Data = idleRecord,
                AttemptCount = 1
            });
            _logger.LogWarning("Idle record queued for retry");
        }
    }

    private async Task SyncPendingDataAsync()
    {
        if (_config == null) return;

        // retry login if not recorded
        var today = DateOnly.FromDateTime(DateTime.Now);
        if (!_loginRecordedToday || _lastLoginDate != today)
        {
            await RecordLoginAsync();
        }

        // retry pending idle records
        var pending = _pendingIdleRecords.ToList();
        foreach (var record in pending)
        {
            var success = await _apiClient.RecordIdleTimeAsync(record.Data);
            if (success)
            {
                _pendingIdleRecords.Remove(record);
            }
            else
            {
                record.AttemptCount++;
                if (record.AttemptCount > 10)
                {
                    _pendingIdleRecords.Remove(record);
                    _logger.LogWarning("Idle record dropped after 10 retries");
                }
            }
        }
    }

    private async Task CheckNotificationsAsync()
    {
        if (_config == null || string.IsNullOrEmpty(_config.Token)) return;

        var notifications = await _apiClient.GetUnreadNotificationsAsync(_config.EmployeeId, _config.Token);
        if (notifications != null && notifications.Count > 0)
        {
            NotificationsReceived?.Invoke(this, notifications);
        }
    }

    private static string GetLocalIpAddress()
    {
        try
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    return ip.ToString();
            }
        }
        catch { }
        return "Unknown";
    }

    private void LoadConfig()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                var json = File.ReadAllText(_configPath);
                _config = JsonSerializer.Deserialize<AgentConfig>(json);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load agent config");
        }
    }

    private void SaveConfig()
    {
        try
        {
            var dir = Path.GetDirectoryName(_configPath)!;
            Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_configPath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save agent config");
        }
    }
}

internal class AgentConfig
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string ServerUrl { get; set; } = string.Empty;
}

internal class PendingIdleRecord
{
    public IdleTimeDto Data { get; set; } = new();
    public int AttemptCount { get; set; }
}
