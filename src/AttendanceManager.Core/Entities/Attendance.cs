using AttendanceManager.Core.Enums;

namespace AttendanceManager.Core.Entities;

public class Attendance
{
    public long Id { get; set; }
    public int EmployeeId { get; set; }
    public DateOnly Date { get; set; }
    public DateTime? LoginTime { get; set; }
    public DateTime? LogoutTime { get; set; }
    public string? ComputerName { get; set; }
    public string? WindowsUsername { get; set; }
    public string? IpAddress { get; set; }
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;
    public double TotalHours { get; set; }
    public double IdleTimeMinutes { get; set; }
    public double EffectiveHours { get; set; }
    public bool IsManualEntry { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Employee? Employee { get; set; }
}
