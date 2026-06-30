using System.Net.Http.Json;
using System.Text.Json;
using AttendanceManager.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace AttendanceManager.Agent.Services;

public class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiClient(HttpClient httpClient, ILogger<ApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    public async Task<ApiResponse<AttendanceDto>?> RecordLoginAsync(AgentLoginRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/attendance/login", request);
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResponse<AttendanceDto>>(content, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record login");
            return null;
        }
    }

    public async Task<ApiResponse<AttendanceDto>?> RecordLogoutAsync(AgentLogoutRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/attendance/logout", request);
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResponse<AttendanceDto>>(content, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record logout");
            return null;
        }
    }

    public async Task<bool> RecordIdleTimeAsync(IdleTimeDto request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/attendance/idle", request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record idle time");
            return false;
        }
    }

    public async Task<LoginResponse?> AuthenticateAsync(LoginRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<LoginResponse>(content, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to authenticate");
            return null;
        }
    }

    public async Task<List<NotificationDto>?> GetUnreadNotificationsAsync(int employeeId, string token)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"api/notification/unread/{employeeId}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<List<NotificationDto>>>(content, _jsonOptions);
            return result?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notifications");
            return null;
        }
    }
}
