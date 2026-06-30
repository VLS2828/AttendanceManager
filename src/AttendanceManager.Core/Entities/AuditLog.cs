namespace AttendanceManager.Core.Entities;

public class AuditLog
{
    public long Id { get; set; }
    public int EmployeeId { get; set; }
    public DateOnly Date { get; set; }
    public string TableName { get; set; } = string.Empty;
    public long RecordId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string? PreviousValue { get; set; }
    public string? NewValue { get; set; }
    public int AdminId { get; set; }
    public string AdminName { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public Employee? Employee { get; set; }
}
