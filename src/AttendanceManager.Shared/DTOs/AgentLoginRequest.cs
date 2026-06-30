namespace AttendanceManager.Shared.DTOs;

public class AgentLoginRequest
{
    public int EmployeeId { get; set; }
    public string ComputerName { get; set; } = string.Empty;
    public string WindowsUsername { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
}
