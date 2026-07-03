using AttendanceManager.Core.Entities;
using AttendanceManager.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AttendanceManager.Infrastructure.Services;

public class WorkSummaryService : IWorkSummaryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<WorkSummaryService> _logger;

    public WorkSummaryService(IUnitOfWork unitOfWork, ILogger<WorkSummaryService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<WorkSummary> SubmitAsync(int employeeId, DateOnly date, List<WorkSummaryEntry> entries)
    {
        var summary = await _unitOfWork.WorkSummaries.FirstOrDefaultAsync(
            w => w.EmployeeId == employeeId && w.Date == date);

        if (summary == null)
        {
            summary = new WorkSummary { EmployeeId = employeeId, Date = date };
            await _unitOfWork.WorkSummaries.AddAsync(summary);
            await _unitOfWork.SaveChangesAsync();
        }
        else
        {
            var existingEntries = await _unitOfWork.WorkSummaryEntries.FindAsync(e => e.WorkSummaryId == summary.Id);
            foreach (var e in existingEntries)
                _unitOfWork.WorkSummaryEntries.Remove(e);
            summary.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.WorkSummaries.Update(summary);
        }

        foreach (var entry in entries)
        {
            entry.WorkSummaryId = summary.Id;
            await _unitOfWork.WorkSummaryEntries.AddAsync(entry);
        }

        await _unitOfWork.SaveChangesAsync();
        summary.Entries = entries;

        _logger.LogInformation("Work summary submitted for Employee {EmpId} on {Date} with {Count} entries",
            employeeId, date, entries.Count);
        return summary;
    }

    public async Task<WorkSummary?> GetByDateAsync(int employeeId, DateOnly date)
    {
        var summary = await _unitOfWork.WorkSummaries.FirstOrDefaultAsync(
            w => w.EmployeeId == employeeId && w.Date == date);
        if (summary == null) return null;

        var entries = await _unitOfWork.WorkSummaryEntries.FindAsync(e => e.WorkSummaryId == summary.Id);
        summary.Entries = entries.ToList();
        return summary;
    }

    public async Task<IEnumerable<WorkSummary>> GetByRangeAsync(int employeeId, DateOnly startDate, DateOnly endDate)
    {
        var summaries = (await _unitOfWork.WorkSummaries.FindAsync(
            w => w.EmployeeId == employeeId && w.Date >= startDate && w.Date <= endDate)).ToList();

        foreach (var summary in summaries)
        {
            var entries = await _unitOfWork.WorkSummaryEntries.FindAsync(e => e.WorkSummaryId == summary.Id);
            summary.Entries = entries.ToList();
        }

        return summaries;
    }
}
