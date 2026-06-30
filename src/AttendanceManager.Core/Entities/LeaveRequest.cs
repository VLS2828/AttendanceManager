using AttendanceManager.Core.Enums;

namespace AttendanceManager.Core.Entities;

public class LeaveRequest
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public int LeaveTypeId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int TotalDays { get; set; }
    public string? Reason { get; set; }
    public LeaveStatus Status { get; set; } = LeaveStatus.Pending;
    public int? ApprovedById { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? AdminRemarks { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Employee? Employee { get; set; }
    public LeaveType? LeaveType { get; set; }
    public Employee? ApprovedBy { get; set; }
}
