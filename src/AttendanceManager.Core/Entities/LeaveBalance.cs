namespace AttendanceManager.Core.Entities;

public class LeaveBalance
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public int LeaveTypeId { get; set; }
    public int Year { get; set; }
    public int TotalDays { get; set; }
    public int UsedDays { get; set; }
    public int RemainingDays => TotalDays - UsedDays;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Employee? Employee { get; set; }
    public LeaveType? LeaveType { get; set; }
}
