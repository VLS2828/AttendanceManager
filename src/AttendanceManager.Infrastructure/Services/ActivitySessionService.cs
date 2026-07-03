using AttendanceManager.Core.Entities;
using AttendanceManager.Core.Enums;
using AttendanceManager.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AttendanceManager.Infrastructure.Services;

public class ActivitySessionService : IActivitySessionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ActivitySessionService> _logger;

    public ActivitySessionService(IUnitOfWork unitOfWork, ILogger<ActivitySessionService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task SyncAsync(int employeeId, List<ActivitySession> intervals)
    {
        if (intervals.Count == 0) return;

        var affectedDates = new HashSet<DateOnly>();
        foreach (var interval in intervals)
        {
            interval.EmployeeId = employeeId;
            interval.Date = DateOnly.FromDateTime(interval.IntervalStart);
            affectedDates.Add(interval.Date);
        }

        await _unitOfWork.ActivitySessions.AddRangeAsync(intervals);
        await _unitOfWork.SaveChangesAsync();

        foreach (var date in affectedDates)
            await RecalculateFromSessionsAsync(employeeId, date);

        _logger.LogInformation("Synced {Count} activity intervals for Employee {EmpId}", intervals.Count, employeeId);
    }

    private async Task RecalculateFromSessionsAsync(int employeeId, DateOnly date)
    {
        var sessions = await _unitOfWork.ActivitySessions.FindAsync(
            s => s.EmployeeId == employeeId && s.Date == date);

        double idleMinutes = sessions
            .Where(s => s.State == ActivityState.Idle)
            .Sum(s => (s.IntervalEnd - s.IntervalStart).TotalMinutes);

        var attendance = await _unitOfWork.Attendances.FirstOrDefaultAsync(
            a => a.EmployeeId == employeeId && a.Date == date);

        if (attendance != null)
        {
            attendance.IdleTimeMinutes = Math.Round(idleMinutes, 2);
            attendance.EffectiveHours = Math.Round(attendance.TotalHours - (idleMinutes / 60.0), 2);
            if (attendance.EffectiveHours < 0) attendance.EffectiveHours = 0;
            attendance.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Attendances.Update(attendance);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
