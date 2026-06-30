using AttendanceManager.Core.Entities;
using AttendanceManager.Core.Enums;
using AttendanceManager.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AttendanceManager.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IUnitOfWork unitOfWork, ILogger<NotificationService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task SendNotificationAsync(int employeeId, string title, string message, NotificationType type)
    {
        var notification = new Notification
        {
            EmployeeId = employeeId,
            Title = title,
            Message = message,
            Type = type,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Notifications.AddAsync(notification);
        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Notification sent to Employee {Id}: {Title}", employeeId, title);
    }

    public async Task<IEnumerable<Notification>> GetUnreadNotificationsAsync(int employeeId) =>
        await _unitOfWork.Notifications.FindAsync(n => n.EmployeeId == employeeId && !n.IsRead);

    public async Task<IEnumerable<Notification>> GetAllNotificationsAsync(int employeeId, int page = 1, int pageSize = 20)
    {
        var all = await _unitOfWork.Notifications.FindAsync(n => n.EmployeeId == employeeId);
        return all.OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    public async Task<Notification?> GetNotificationByIdAsync(long notificationId) =>
        await _unitOfWork.Notifications.GetByLongIdAsync(notificationId);

    public async Task MarkAsReadAsync(long notificationId)
    {
        var notification = await _unitOfWork.Notifications.GetByLongIdAsync(notificationId);
        if (notification != null)
        {
            notification.IsRead = true;
            _unitOfWork.Notifications.Update(notification);
            await _unitOfWork.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsReadAsync(int employeeId)
    {
        var unread = await _unitOfWork.Notifications.FindAsync(n => n.EmployeeId == employeeId && !n.IsRead);
        foreach (var n in unread)
        {
            n.IsRead = true;
            _unitOfWork.Notifications.Update(n);
        }
        await _unitOfWork.SaveChangesAsync();
    }
}
