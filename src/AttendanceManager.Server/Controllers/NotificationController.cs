using AttendanceManager.Core.Interfaces;
using AttendanceManager.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceManager.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet("unread/{employeeId}")]
    public async Task<ActionResult<ApiResponse<List<NotificationDto>>>> GetUnread(int employeeId)
    {
        var currentEmployeeId = int.Parse(User.FindFirst("EmployeeId")?.Value ?? "0");
        if (currentEmployeeId != employeeId && !User.IsInRole("Admin"))
            return Forbid();

        var notifications = await _notificationService.GetUnreadNotificationsAsync(employeeId);
        return Ok(ApiResponse<List<NotificationDto>>.Ok(
            notifications.Select(n => new NotificationDto
            {
                Id = n.Id, Title = n.Title, Message = n.Message,
                Type = n.Type.ToString(), IsRead = n.IsRead,
                CreatedAt = n.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
            }).ToList()));
    }

    [HttpGet("{employeeId}")]
    public async Task<ActionResult<ApiResponse<List<NotificationDto>>>> GetAll(
        int employeeId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var currentEmployeeId = int.Parse(User.FindFirst("EmployeeId")?.Value ?? "0");
        if (currentEmployeeId != employeeId && !User.IsInRole("Admin"))
            return Forbid();

        var notifications = await _notificationService.GetAllNotificationsAsync(employeeId, page, pageSize);
        return Ok(ApiResponse<List<NotificationDto>>.Ok(
            notifications.Select(n => new NotificationDto
            {
                Id = n.Id, Title = n.Title, Message = n.Message,
                Type = n.Type.ToString(), IsRead = n.IsRead,
                CreatedAt = n.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
            }).ToList()));
    }

    [HttpPost("{id}/read")]
    public async Task<ActionResult<ApiResponse>> MarkAsRead(long id)
    {
        var currentEmployeeId = int.Parse(User.FindFirst("EmployeeId")?.Value ?? "0");
        var notification = await _notificationService.GetNotificationByIdAsync(id);
        if (notification != null && notification.EmployeeId != currentEmployeeId && !User.IsInRole("Admin"))
            return Forbid();

        await _notificationService.MarkAsReadAsync(id);
        return Ok(ApiResponse.Ok("Notification marked as read."));
    }

    [HttpPost("{employeeId}/read-all")]
    public async Task<ActionResult<ApiResponse>> MarkAllAsRead(int employeeId)
    {
        var currentEmployeeId = int.Parse(User.FindFirst("EmployeeId")?.Value ?? "0");
        if (currentEmployeeId != employeeId && !User.IsInRole("Admin"))
            return Forbid();

        await _notificationService.MarkAllAsReadAsync(employeeId);
        return Ok(ApiResponse.Ok("All notifications marked as read."));
    }
}
