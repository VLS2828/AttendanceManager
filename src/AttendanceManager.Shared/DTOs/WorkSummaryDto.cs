namespace AttendanceManager.Shared.DTOs;

public class WorkSummaryEntryDto
{
    public string FromTime { get; set; } = string.Empty;
    public string ToTime { get; set; } = string.Empty;
    public string Client { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class WorkSummaryDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string Date { get; set; } = string.Empty;
    public List<WorkSummaryEntryDto> Entries { get; set; } = new();
}

public class SubmitWorkSummaryRequest
{
    public int EmployeeId { get; set; }
    public string Date { get; set; } = string.Empty;
    public List<WorkSummaryEntryDto> Entries { get; set; } = new();
}
