namespace AttendanceManager.Core.Entities;

public class IdleLog
{
    public long Id { get; set; }
    public int EmployeeId { get; set; }
    public DateOnly Date { get; set; }
    public DateTime IdleStartTime { get; set; }
    public DateTime IdleEndTime { get; set; }
    public double DurationMinutes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Employee? Employee { get; set; }
}
