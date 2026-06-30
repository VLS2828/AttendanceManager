namespace AttendanceManager.Core.Entities;

public class Holiday
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public string? Description { get; set; }
    public bool IsOptional { get; set; }
    public int Year { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
