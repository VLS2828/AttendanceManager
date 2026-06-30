namespace AttendanceManager.Shared.DTOs;

public class AttendanceDto
{
    public long Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string? LoginTime { get; set; }
    public string? LogoutTime { get; set; }
    public string? ComputerName { get; set; }
    public string? WindowsUsername { get; set; }
    public string? IpAddress { get; set; }
    public string Status { get; set; } = string.Empty;
    public double TotalHours { get; set; }
    public double IdleTimeMinutes { get; set; }
    public double EffectiveHours { get; set; }
    public bool IsManualEntry { get; set; }
    public string? Remarks { get; set; }
}
