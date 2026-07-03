namespace AttendanceManager.Shared.DTOs;

public class ActivityIntervalDto
{
    public string Start { get; set; } = string.Empty;
    public string End { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}

public class ActivitySessionSyncRequest
{
    public int EmployeeId { get; set; }
    public List<ActivityIntervalDto> Intervals { get; set; } = new();
}
