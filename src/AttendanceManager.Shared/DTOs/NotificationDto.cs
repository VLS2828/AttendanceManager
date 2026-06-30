namespace AttendanceManager.Shared.DTOs;

public class NotificationDto
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
}
