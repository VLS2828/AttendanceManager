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
    private string _authToken = string.Empty;

    public ApiClient(HttpClient httpClient, ILogger<ApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    public void SetAuthToken(string token)
    {
        _authToken = token;
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string url, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (!string.IsNullOrEmpty(_authToken))
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authToken);
        if (body != null)
            request.Content = System.Net.Http.Json.JsonContent.Create(body);
        return request;
    }

    public async Task<ApiResponse<AttendanceDto>?> RecordLoginAsync(AgentLoginRequest request)
    {
        try
        {
            var httpRequest = CreateRequest(HttpMethod.Post, "api/attendance/login", request);
            var response = await _httpClient.SendAsync(httpRequest);
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
            var httpRequest = CreateRequest(HttpMethod.Post, "api/attendance/logout", request);
            var response = await _httpClient.SendAsync(httpRequest);
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
            var httpRequest = CreateRequest(HttpMethod.Post, "api/attendance/idle", request);
            var response = await _httpClient.SendAsync(httpRequest);
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

    public async Task<LoginResponse?> RefreshTokenAsync(string token)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/refresh");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<LoginResponse>(content, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh token");
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
