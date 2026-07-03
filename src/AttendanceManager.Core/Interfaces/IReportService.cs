using AttendanceManager.Core.Entities;

namespace AttendanceManager.Core.Interfaces;

public interface IReportService
{
    Task<IEnumerable<Attendance>> GetDailyAttendanceReportAsync(DateOnly date);
    Task<IEnumerable<Attendance>> GetAttendanceReportAsync(DateOnly startDate, DateOnly endDate);
    Task<IEnumerable<Attendance>> GetMonthlyAttendanceReportAsync(int employeeId, int year, int month);
    Task<IEnumerable<Attendance>> GetLateArrivalsReportAsync(DateOnly startDate, DateOnly endDate, TimeOnly threshold);
    Task<IEnumerable<Attendance>> GetOvertimeReportAsync(DateOnly startDate, DateOnly endDate, double standardHours);
    Task<IEnumerable<Attendance>> GetIdleTimeReportAsync(DateOnly startDate, DateOnly endDate);
    Task<IEnumerable<LeaveRequest>> GetLeaveSummaryReportAsync(DateOnly startDate, DateOnly endDate);
    Task<IEnumerable<Holiday>> GetHolidaySummaryReportAsync(int year);
    Task<IEnumerable<Attendance>> GetEmployeeWorkingHoursReportAsync(int employeeId, DateOnly startDate, DateOnly endDate);
    Task<Dictionary<string, object>> GetDepartmentSummaryAsync(DateOnly startDate, DateOnly endDate);
}
