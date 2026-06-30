using AttendanceManager.Core.Entities;
using AttendanceManager.Core.Enums;

namespace AttendanceManager.Core.Interfaces;

public interface INotificationService
{
    Task SendNotificationAsync(int employeeId, string title, string message, NotificationType type);
    Task<IEnumerable<Notification>> GetUnreadNotificationsAsync(int employeeId);
    Task<IEnumerable<Notification>> GetAllNotificationsAsync(int employeeId, int page = 1, int pageSize = 20);
    Task MarkAsReadAsync(long notificationId);
    Task MarkAllAsReadAsync(int employeeId);
}
