namespace AttendanceManager.Core.Entities;

public class WorkSummary
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public DateOnly Date { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Employee? Employee { get; set; }
    public ICollection<WorkSummaryEntry> Entries { get; set; } = new List<WorkSummaryEntry>();
}
