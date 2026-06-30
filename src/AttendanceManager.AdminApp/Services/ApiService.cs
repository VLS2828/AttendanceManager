using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AttendanceManager.Shared.DTOs;

namespace AttendanceManager.AdminApp.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private string _token = string.Empty;
    private int _employeeId;
    private string _role = string.Empty;
    private string _fullName = string.Empty;

    public int EmployeeId => _employeeId;
    public string Role => _role;
    public string FullName => _fullName;
    public bool IsAdmin => _role == "Admin";

    public ApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    public void SetBaseUrl(string url)
    {
        _httpClient.BaseAddress = new Uri(url);
    }

    private void SetAuthHeader()
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
    }

    public async Task<LoginResponse> LoginAsync(string email, string password)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        });

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<LoginResponse>(content, _jsonOptions)!;

        if (result.Success)
        {
            _token = result.Token ?? "";
            _employeeId = result.EmployeeId;
            _role = result.Role;
            _fullName = result.FullName;
            SetAuthHeader();
        }

        return result;
    }

    public async Task<AdminDashboardDto?> GetAdminDashboardAsync()
    {
        var result = await GetAsync<AdminDashboardDto>("api/dashboard/admin");
        return result?.Data;
    }

    public async Task<EmployeeDashboardDto?> GetEmployeeDashboardAsync(int employeeId)
    {
        var result = await GetAsync<EmployeeDashboardDto>($"api/dashboard/employee/{employeeId}");
        return result?.Data;
    }

    public async Task<List<EmployeeDto>> GetAllEmployeesAsync()
    {
        var result = await GetAsync<List<EmployeeDto>>("api/employee");
        return result?.Data ?? new();
    }

    public async Task<ApiResponse<EmployeeDto>?> CreateEmployeeAsync(object request)
    {
        return await PostAsync<EmployeeDto>("api/employee", request);
    }

    public async Task<List<AttendanceDto>> GetDailyAttendanceAsync(string date)
    {
        var result = await GetAsync<List<AttendanceDto>>($"api/attendance/daily?date={date}");
        return result?.Data ?? new();
    }

    public async Task<List<AttendanceDto>> GetAttendanceRangeAsync(int employeeId, string startDate, string endDate)
    {
        var result = await GetAsync<List<AttendanceDto>>($"api/attendance/range/{employeeId}?startDate={startDate}&endDate={endDate}");
        return result?.Data ?? new();
    }

    public async Task<ApiResponse?> UpdateAttendanceStatusAsync(long id, string status, string reason)
    {
        return await PutAsync($"api/attendance/{id}/status?status={status}&reason={Uri.EscapeDataString(reason)}");
    }

    public async Task<ApiResponse?> UpdateLoginTimeAsync(long id, string loginTime, string reason)
    {
        return await PutAsync($"api/attendance/{id}/login-time?loginTime={Uri.EscapeDataString(loginTime)}&reason={Uri.EscapeDataString(reason)}");
    }

    public async Task<ApiResponse?> UpdateLogoutTimeAsync(long id, string logoutTime, string reason)
    {
        return await PutAsync($"api/attendance/{id}/logout-time?logoutTime={Uri.EscapeDataString(logoutTime)}&reason={Uri.EscapeDataString(reason)}");
    }

    public async Task<ApiResponse?> InsertManualAttendanceAsync(AttendanceDto dto, string reason)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/attendance/manual?reason={Uri.EscapeDataString(reason)}", dto);
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ApiResponse>(content, _jsonOptions);
    }

    public async Task<ApiResponse?> DeleteAttendanceAsync(long id, string reason)
    {
        var response = await _httpClient.DeleteAsync($"api/attendance/{id}?reason={Uri.EscapeDataString(reason)}");
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ApiResponse>(content, _jsonOptions);
    }

    public async Task<List<LeaveRequestDto>> GetPendingLeavesAsync()
    {
        var result = await GetAsync<List<LeaveRequestDto>>("api/leave/pending");
        return result?.Data ?? new();
    }

    public async Task<List<LeaveRequestDto>> GetEmployeeLeavesAsync(int employeeId)
    {
        var result = await GetAsync<List<LeaveRequestDto>>($"api/leave/employee/{employeeId}");
        return result?.Data ?? new();
    }

    public async Task<ApiResponse?> SubmitLeaveRequestAsync(object request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/leave/request", request);
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ApiResponse>(content, _jsonOptions);
    }

    public async Task<ApiResponse?> ApproveLeaveAsync(int id, string? remarks)
    {
        var url = $"api/leave/{id}/approve" + (remarks != null ? $"?remarks={Uri.EscapeDataString(remarks)}" : "");
        var response = await _httpClient.PostAsync(url, null);
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ApiResponse>(content, _jsonOptions);
    }

    public async Task<ApiResponse?> RejectLeaveAsync(int id, string? remarks)
    {
        var url = $"api/leave/{id}/reject" + (remarks != null ? $"?remarks={Uri.EscapeDataString(remarks)}" : "");
        var response = await _httpClient.PostAsync(url, null);
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ApiResponse>(content, _jsonOptions);
    }

    public async Task<ApiResponse?> CancelLeaveAsync(int id, string? remarks)
    {
        var url = $"api/leave/{id}/cancel" + (remarks != null ? $"?remarks={Uri.EscapeDataString(remarks)}" : "");
        var response = await _httpClient.PostAsync(url, null);
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ApiResponse>(content, _jsonOptions);
    }

    public async Task<List<LeaveBalanceDto>> GetLeaveBalancesAsync(int employeeId, int? year = null)
    {
        var url = $"api/leave/balance/{employeeId}" + (year.HasValue ? $"?year={year}" : "");
        var result = await GetAsync<List<LeaveBalanceDto>>(url);
        return result?.Data ?? new();
    }

    public async Task<List<NotificationDto>> GetNotificationsAsync(int employeeId)
    {
        var result = await GetAsync<List<NotificationDto>>($"api/notification/{employeeId}");
        return result?.Data ?? new();
    }

    public async Task<List<NotificationDto>> GetUnreadNotificationsAsync(int employeeId)
    {
        var result = await GetAsync<List<NotificationDto>>($"api/notification/unread/{employeeId}");
        return result?.Data ?? new();
    }

    public async Task MarkNotificationAsReadAsync(long id)
    {
        await _httpClient.PostAsync($"api/notification/{id}/read", null);
    }

    public async Task<List<object>> GetHolidaysAsync(int? year = null)
    {
        var y = year ?? DateTime.Now.Year;
        var result = await GetAsync<List<object>>($"api/holiday?year={y}");
        return result?.Data ?? new();
    }

    public async Task<List<object>> GetDepartmentsAsync()
    {
        var result = await GetAsync<List<object>>("api/department");
        return result?.Data ?? new();
    }

    public async Task<List<AttendanceDto>> GetReportAsync(string reportType, Dictionary<string, string> parameters)
    {
        var query = string.Join("&", parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
        var result = await GetAsync<List<AttendanceDto>>($"api/report/{reportType}?{query}");
        return result?.Data ?? new();
    }

    public async Task<List<object>> GetAuditLogsAsync(int? employeeId, string? startDate, string? endDate)
    {
        var parameters = new List<string>();
        if (employeeId.HasValue) parameters.Add($"employeeId={employeeId}");
        if (startDate != null) parameters.Add($"startDate={startDate}");
        if (endDate != null) parameters.Add($"endDate={endDate}");

        var query = parameters.Count > 0 ? "?" + string.Join("&", parameters) : "";
        var result = await GetAsync<List<object>>($"api/audit{query}");
        return result?.Data ?? new();
    }

    private async Task<ApiResponse<T>?> GetAsync<T>(string url)
    {
        try
        {
            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResponse<T>>(content, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private async Task<ApiResponse<T>?> PostAsync<T>(string url, object data)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(url, data);
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResponse<T>>(content, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private async Task<ApiResponse?> PutAsync(string url)
    {
        try
        {
            var response = await _httpClient.PutAsync(url, null);
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResponse>(content, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }
}
