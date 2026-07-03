namespace AttendanceManager.Shared.DTOs;

public class ApprovalItemDto
{
    public string Type { get; set; } = string.Empty; // "Leave" or "Correction"
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string RequestedAt { get; set; } = string.Empty;
}
