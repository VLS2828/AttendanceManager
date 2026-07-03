using AttendanceManager.Core.Entities;

namespace AttendanceManager.Core.Interfaces;

public interface IWorkSummaryService
{
    Task<WorkSummary> SubmitAsync(int employeeId, DateOnly date, List<WorkSummaryEntry> entries);
    Task<WorkSummary?> GetByDateAsync(int employeeId, DateOnly date);
    Task<IEnumerable<WorkSummary>> GetByRangeAsync(int employeeId, DateOnly startDate, DateOnly endDate);
}
