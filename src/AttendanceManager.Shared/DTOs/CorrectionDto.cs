namespace AttendanceManager.Shared.DTOs;

public class CorrectionDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string ClaimedStatus { get; set; } = string.Empty;
    public string? ClaimedLoginTime { get; set; }
    public string? ClaimedLogoutTime { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? AdminComment { get; set; }
    public string RequestedAt { get; set; } = string.Empty;
    public string? DecidedAt { get; set; }
    public string? DecidedByName { get; set; }
}

public class SubmitCorrectionRequest
{
    public int EmployeeId { get; set; }
    public string Date { get; set; } = string.Empty;
    public string ClaimedStatus { get; set; } = string.Empty;
    public string? ClaimedLoginTime { get; set; }
    public string? ClaimedLogoutTime { get; set; }
    public string Reason { get; set; } = string.Empty;
}
