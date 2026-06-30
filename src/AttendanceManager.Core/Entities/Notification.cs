using AttendanceManager.Core.Enums;

namespace AttendanceManager.Core.Entities;

public class Notification
{
    public long Id { get; set; }
    public int EmployeeId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Employee? Employee { get; set; }
}
