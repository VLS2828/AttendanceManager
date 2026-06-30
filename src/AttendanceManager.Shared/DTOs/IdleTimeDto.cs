namespace AttendanceManager.Shared.DTOs;

public class IdleTimeDto
{
    public int EmployeeId { get; set; }
    public DateTime IdleStartTime { get; set; }
    public DateTime IdleEndTime { get; set; }
    public double DurationMinutes { get; set; }
}
