using AttendanceManager.Core.Entities;
using AttendanceManager.Core.Enums;

namespace AttendanceManager.Core.Interfaces;

public interface IAttendanceService
{
    Task<Attendance?> RecordLoginAsync(int employeeId, string computerName, string windowsUsername, string? ipAddress);
    Task<Attendance?> RecordLogoutAsync(int employeeId);
    Task<Attendance?> GetTodayAttendanceAsync(int employeeId);
    Task<IEnumerable<Attendance>> GetAttendanceByDateRangeAsync(int employeeId, DateOnly startDate, DateOnly endDate);
    Task<IEnumerable<Attendance>> GetAllAttendanceByDateAsync(DateOnly date);
    Task UpdateAttendanceStatusAsync(long attendanceId, AttendanceStatus status, int adminId, string reason);
    Task UpdateLoginTimeAsync(long attendanceId, DateTime loginTime, int adminId, string reason);
    Task UpdateLogoutTimeAsync(long attendanceId, DateTime logoutTime, int adminId, string reason);
    Task InsertManualAttendanceAsync(Attendance attendance, int adminId, string reason);
    Task DeleteAttendanceAsync(long attendanceId, int adminId, string reason);
    Task RecordIdleTimeAsync(int employeeId, DateTime idleStart, DateTime idleEnd);
    Task RecalculateEffectiveHoursAsync(long attendanceId);
    Task ProcessDailyAttendanceStatusAsync(DateOnly date);
}
