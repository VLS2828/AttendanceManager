using AttendanceManager.Core.Entities;
using AttendanceManager.Core.Enums;
using AttendanceManager.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AttendanceManager.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReportService> _logger;

    public ReportService(IUnitOfWork unitOfWork, ILogger<ReportService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IEnumerable<Attendance>> GetDailyAttendanceReportAsync(DateOnly date) =>
        await _unitOfWork.Attendances.FindAsync(a => a.Date == date);

    public async Task<IEnumerable<Attendance>> GetAttendanceReportAsync(DateOnly startDate, DateOnly endDate) =>
        await _unitOfWork.Attendances.FindAsync(a => a.Date >= startDate && a.Date <= endDate);

    public async Task<IEnumerable<Attendance>> GetMonthlyAttendanceReportAsync(int employeeId, int year, int month)
    {
        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);
        return await _unitOfWork.Attendances.FindAsync(
            a => a.EmployeeId == employeeId && a.Date >= startDate && a.Date <= endDate);
    }

    public async Task<IEnumerable<Attendance>> GetLateArrivalsReportAsync(DateOnly startDate, DateOnly endDate, TimeOnly threshold)
    {
        var attendances = await _unitOfWork.Attendances.FindAsync(
            a => a.Date >= startDate && a.Date <= endDate && a.LoginTime != null && a.Status == AttendanceStatus.Present);

        return attendances.Where(a =>
        {
            var loginTimeOfDay = TimeOnly.FromDateTime(a.LoginTime!.Value);
            return loginTimeOfDay > threshold;
        }).ToList();
    }

    public async Task<IEnumerable<Attendance>> GetOvertimeReportAsync(DateOnly startDate, DateOnly endDate, double standardHours)
    {
        var attendances = await _unitOfWork.Attendances.FindAsync(
            a => a.Date >= startDate && a.Date <= endDate && a.Status == AttendanceStatus.Present);
        return attendances.Where(a => a.TotalHours > standardHours).ToList();
    }

    public async Task<IEnumerable<Attendance>> GetIdleTimeReportAsync(DateOnly startDate, DateOnly endDate) =>
        await _unitOfWork.Attendances.FindAsync(
            a => a.Date >= startDate && a.Date <= endDate && a.IdleTimeMinutes > 0);

    public async Task<IEnumerable<LeaveRequest>> GetLeaveSummaryReportAsync(DateOnly startDate, DateOnly endDate) =>
        await _unitOfWork.LeaveRequests.FindAsync(lr => lr.StartDate <= endDate && lr.EndDate >= startDate);

    public async Task<IEnumerable<Holiday>> GetHolidaySummaryReportAsync(int year) =>
        await _unitOfWork.Holidays.FindAsync(h => h.Year == year);

    public async Task<IEnumerable<Attendance>> GetEmployeeWorkingHoursReportAsync(int employeeId, DateOnly startDate, DateOnly endDate) =>
        await _unitOfWork.Attendances.FindAsync(
            a => a.EmployeeId == employeeId && a.Date >= startDate && a.Date <= endDate);

    public async Task<Dictionary<string, object>> GetDepartmentSummaryAsync(DateOnly startDate, DateOnly endDate)
    {
        var departments = await _unitOfWork.Departments.GetAllAsync();
        var result = new Dictionary<string, object>();

        foreach (var dept in departments)
        {
            var employees = await _unitOfWork.Employees.FindAsync(e => e.DepartmentId == dept.Id && e.IsActive);
            var empIds = employees.Select(e => e.Id).ToList();

            var attendances = await _unitOfWork.Attendances.FindAsync(
                a => empIds.Contains(a.EmployeeId) && a.Date >= startDate && a.Date <= endDate);

            var attendanceList = attendances.ToList();
            result[dept.Name] = new
            {
                TotalEmployees = empIds.Count,
                TotalPresent = attendanceList.Count(a => a.Status == AttendanceStatus.Present),
                TotalAbsent = attendanceList.Count(a => a.Status == AttendanceStatus.Absent),
                TotalLeave = attendanceList.Count(a => a.Status == AttendanceStatus.Leave),
                AverageWorkingHours = attendanceList.Where(a => a.TotalHours > 0).DefaultIfEmpty().Average(a => a?.TotalHours ?? 0),
                TotalIdleMinutes = attendanceList.Sum(a => a.IdleTimeMinutes)
            };
        }

        return result;
    }
}
