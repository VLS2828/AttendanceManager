using AttendanceManager.Core.Enums;

namespace AttendanceManager.Core.Entities;

public class Correction
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public DateOnly Date { get; set; }
    public ClaimedStatus ClaimedStatus { get; set; }
    public DateTime? ClaimedLoginTime { get; set; }
    public DateTime? ClaimedLogoutTime { get; set; }
    public string Reason { get; set; } = string.Empty;
    public CorrectionStatus Status { get; set; } = CorrectionStatus.Pending;
    public string? AdminComment { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DecidedAt { get; set; }
    public int? DecidedById { get; set; }

    public Employee? Employee { get; set; }
    public Employee? DecidedBy { get; set; }
}
