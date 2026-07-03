namespace AttendanceManager.Core.Entities;

public class WorkSummaryEntry
{
    public int Id { get; set; }
    public int WorkSummaryId { get; set; }
    public TimeOnly FromTime { get; set; }
    public TimeOnly ToTime { get; set; }
    public string Client { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public WorkSummary? WorkSummary { get; set; }
}
