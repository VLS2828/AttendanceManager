namespace AttendanceManager.Shared.DTOs;

public class LeaveRequestDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int LeaveTypeId { get; set; }
    public string LeaveTypeName { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public int TotalDays { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ApprovedByName { get; set; }
    public string? ApprovedAt { get; set; }
    public string? AdminRemarks { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
}
