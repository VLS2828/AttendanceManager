using AttendanceManager.Core.Entities;
using AttendanceManager.Core.Enums;
using AttendanceManager.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AttendanceManager.Infrastructure.Services;

public class AttendanceService : IAttendanceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ILogger<AttendanceService> _logger;

    public AttendanceService(IUnitOfWork unitOfWork, IAuditService auditService, ILogger<AttendanceService> logger)
    {
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<Attendance?> RecordLoginAsync(int employeeId, string computerName, string windowsUsername, string? ipAddress)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var existing = await _unitOfWork.Attendances.FirstOrDefaultAsync(
            a => a.EmployeeId == employeeId && a.Date == today);

        if (existing != null)
        {
            _logger.LogInformation("Attendance already recorded for Employee {EmpId} on {Date}", employeeId, today);
            return existing;
        }

        var attendance = new Attendance
        {
            EmployeeId = employeeId,
            Date = today,
            LoginTime = DateTime.Now,
            ComputerName = computerName,
            WindowsUsername = windowsUsername,
            IpAddress = ipAddress,
            Status = AttendanceStatus.Present
        };

        await _unitOfWork.Attendances.AddAsync(attendance);
        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Login recorded for Employee {EmpId} at {Time}", employeeId, attendance.LoginTime);
        return attendance;
    }

    public async Task<Attendance?> RecordLogoutAsync(int employeeId)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var attendance = await _unitOfWork.Attendances.FirstOrDefaultAsync(
            a => a.EmployeeId == employeeId && a.Date == today);

        if (attendance == null)
        {
            _logger.LogWarning("No attendance found for Employee {EmpId} on {Date} for logout", employeeId, today);
            return null;
        }

        attendance.LogoutTime = DateTime.Now;

        if (attendance.LoginTime.HasValue)
        {
            var totalSpan = attendance.LogoutTime.Value - attendance.LoginTime.Value;
            attendance.TotalHours = Math.Round(totalSpan.TotalHours, 2);
            attendance.EffectiveHours = Math.Round(attendance.TotalHours - (attendance.IdleTimeMinutes / 60.0), 2);
            if (attendance.EffectiveHours < 0) attendance.EffectiveHours = 0;
        }

        attendance.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Attendances.Update(attendance);
        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Logout recorded for Employee {EmpId} at {Time}. Total: {Hours}h",
            employeeId, attendance.LogoutTime, attendance.TotalHours);
        return attendance;
    }

    public async Task<Attendance?> GetTodayAttendanceAsync(int employeeId)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        return await _unitOfWork.Attendances.FirstOrDefaultAsync(
            a => a.EmployeeId == employeeId && a.Date == today);
    }

    public async Task<IEnumerable<Attendance>> GetAttendanceByDateRangeAsync(int employeeId, DateOnly startDate, DateOnly endDate)
    {
        return await _unitOfWork.Attendances.FindAsync(
            a => a.EmployeeId == employeeId && a.Date >= startDate && a.Date <= endDate);
    }

    public async Task<IEnumerable<Attendance>> GetAllAttendanceByDateAsync(DateOnly date)
    {
        return await _unitOfWork.Attendances.FindAsync(a => a.Date == date);
    }

    public async Task UpdateAttendanceStatusAsync(long attendanceId, AttendanceStatus status, int adminId, string reason)
    {
        var attendance = await _unitOfWork.Attendances.GetByLongIdAsync(attendanceId);
        if (attendance == null) throw new InvalidOperationException("Attendance record not found.");

        var admin = await _unitOfWork.Employees.GetByIdAsync(adminId);
        string adminName = admin?.FullName ?? "System";

        var previousValue = attendance.Status.ToString();
        attendance.Status = status;
        attendance.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Attendances.Update(attendance);

        await _auditService.LogChangeAsync(attendance.EmployeeId, attendance.Date, "Attendance",
            attendanceId, "Status", previousValue, status.ToString(), adminId, adminName, reason);

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task UpdateLoginTimeAsync(long attendanceId, DateTime loginTime, int adminId, string reason)
    {
        var attendance = await _unitOfWork.Attendances.GetByLongIdAsync(attendanceId);
        if (attendance == null) throw new InvalidOperationException("Attendance record not found.");

        var admin = await _unitOfWork.Employees.GetByIdAsync(adminId);
        string adminName = admin?.FullName ?? "System";

        var previousValue = attendance.LoginTime?.ToString("yyyy-MM-dd HH:mm:ss");
        attendance.LoginTime = loginTime;
        attendance.UpdatedAt = DateTime.UtcNow;
        RecalculateHours(attendance);
        _unitOfWork.Attendances.Update(attendance);

        await _auditService.LogChangeAsync(attendance.EmployeeId, attendance.Date, "Attendance",
            attendanceId, "LoginTime", previousValue, loginTime.ToString("yyyy-MM-dd HH:mm:ss"), adminId, adminName, reason);

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task UpdateLogoutTimeAsync(long attendanceId, DateTime logoutTime, int adminId, string reason)
    {
        var attendance = await _unitOfWork.Attendances.GetByLongIdAsync(attendanceId);
        if (attendance == null) throw new InvalidOperationException("Attendance record not found.");

        var admin = await _unitOfWork.Employees.GetByIdAsync(adminId);
        string adminName = admin?.FullName ?? "System";

        var previousValue = attendance.LogoutTime?.ToString("yyyy-MM-dd HH:mm:ss");
        attendance.LogoutTime = logoutTime;
        attendance.UpdatedAt = DateTime.UtcNow;
        RecalculateHours(attendance);
        _unitOfWork.Attendances.Update(attendance);

        await _auditService.LogChangeAsync(attendance.EmployeeId, attendance.Date, "Attendance",
            attendanceId, "LogoutTime", previousValue, logoutTime.ToString("yyyy-MM-dd HH:mm:ss"), adminId, adminName, reason);

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task InsertManualAttendanceAsync(Attendance attendance, int adminId, string reason)
    {
        attendance.IsManualEntry = true;
        attendance.CreatedAt = DateTime.UtcNow;
        attendance.UpdatedAt = DateTime.UtcNow;
        RecalculateHours(attendance);
        await _unitOfWork.Attendances.AddAsync(attendance);
        await _unitOfWork.SaveChangesAsync();

        var admin = await _unitOfWork.Employees.GetByIdAsync(adminId);
        string adminName = admin?.FullName ?? "System";

        await _auditService.LogChangeAsync(attendance.EmployeeId, attendance.Date, "Attendance",
            attendance.Id, "ManualInsert", null, "Manual entry created", adminId, adminName, reason);
    }

    public async Task DeleteAttendanceAsync(long attendanceId, int adminId, string reason)
    {
        var attendance = await _unitOfWork.Attendances.GetByLongIdAsync(attendanceId);
        if (attendance == null) throw new InvalidOperationException("Attendance record not found.");

        var admin = await _unitOfWork.Employees.GetByIdAsync(adminId);
        string adminName = admin?.FullName ?? "System";

        await _auditService.LogChangeAsync(attendance.EmployeeId, attendance.Date, "Attendance",
            attendanceId, "Delete", $"Login:{attendance.LoginTime} Logout:{attendance.LogoutTime}", "Deleted", adminId, adminName, reason);

        _unitOfWork.Attendances.Remove(attendance);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task RecordIdleTimeAsync(int employeeId, DateTime idleStart, DateTime idleEnd)
    {
        var duration = (idleEnd - idleStart).TotalMinutes;
        if (duration < 6) return; // ignore periods shorter than 6 minutes

        var date = DateOnly.FromDateTime(idleStart);
        var idleLog = new IdleLog
        {
            EmployeeId = employeeId,
            Date = date,
            IdleStartTime = idleStart,
            IdleEndTime = idleEnd,
            DurationMinutes = Math.Round(duration, 2)
        };

        await _unitOfWork.IdleLogs.AddAsync(idleLog);
        await _unitOfWork.SaveChangesAsync();

        await RecalculateIdleTimeForDateAsync(employeeId, date);
    }

    public async Task RecalculateEffectiveHoursAsync(long attendanceId)
    {
        var attendance = await _unitOfWork.Attendances.GetByLongIdAsync(attendanceId);
        if (attendance == null) return;

        RecalculateHours(attendance);
        _unitOfWork.Attendances.Update(attendance);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task ProcessDailyAttendanceStatusAsync(DateOnly date)
    {
        var employees = await _unitOfWork.Employees.FindAsync(e => e.IsActive);
        var holidays = await _unitOfWork.Holidays.FindAsync(h => h.Date == date);
        bool isHoliday = holidays.Any();

        var weeklyOffSetting = await _unitOfWork.AppSettings.FirstOrDefaultAsync(s => s.Key == "WeeklyOffDays");
        var weeklyOffDays = weeklyOffSetting?.Value.Split(',').Select(d => d.Trim()).ToList() ?? new List<string>();
        bool isWeeklyOff = weeklyOffDays.Contains(date.DayOfWeek.ToString());

        foreach (var employee in employees)
        {
            var attendance = await _unitOfWork.Attendances.FirstOrDefaultAsync(
                a => a.EmployeeId == employee.Id && a.Date == date);

            if (attendance != null) continue; // already has attendance

            if (isHoliday)
            {
                await CreateStatusAttendanceAsync(employee.Id, date, AttendanceStatus.Holiday);
            }
            else if (isWeeklyOff)
            {
                await CreateStatusAttendanceAsync(employee.Id, date, AttendanceStatus.WeeklyOff);
            }
            else
            {
                var approvedLeave = await _unitOfWork.LeaveRequests.FirstOrDefaultAsync(
                    lr => lr.EmployeeId == employee.Id &&
                          lr.Status == LeaveStatus.Approved &&
                          lr.StartDate <= date && lr.EndDate >= date);

                var status = approvedLeave != null ? AttendanceStatus.Leave : AttendanceStatus.Absent;
                await CreateStatusAttendanceAsync(employee.Id, date, status);
            }
        }

        await _unitOfWork.SaveChangesAsync();
    }

    private async Task CreateStatusAttendanceAsync(int employeeId, DateOnly date, AttendanceStatus status)
    {
        var attendance = new Attendance
        {
            EmployeeId = employeeId,
            Date = date,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _unitOfWork.Attendances.AddAsync(attendance);
    }

    private async Task RecalculateIdleTimeForDateAsync(int employeeId, DateOnly date)
    {
        var idleLogs = await _unitOfWork.IdleLogs.FindAsync(
            i => i.EmployeeId == employeeId && i.Date == date);
        double totalIdleMinutes = idleLogs.Sum(i => i.DurationMinutes);

        var attendance = await _unitOfWork.Attendances.FirstOrDefaultAsync(
            a => a.EmployeeId == employeeId && a.Date == date);

        if (attendance != null)
        {
            attendance.IdleTimeMinutes = Math.Round(totalIdleMinutes, 2);
            attendance.EffectiveHours = Math.Round(attendance.TotalHours - (totalIdleMinutes / 60.0), 2);
            if (attendance.EffectiveHours < 0) attendance.EffectiveHours = 0;
            attendance.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Attendances.Update(attendance);
            await _unitOfWork.SaveChangesAsync();
        }
    }

    private static void RecalculateHours(Attendance attendance)
    {
        if (attendance.LoginTime.HasValue && attendance.LogoutTime.HasValue)
        {
            var totalSpan = attendance.LogoutTime.Value - attendance.LoginTime.Value;
            attendance.TotalHours = Math.Round(totalSpan.TotalHours, 2);
            attendance.EffectiveHours = Math.Round(attendance.TotalHours - (attendance.IdleTimeMinutes / 60.0), 2);
            if (attendance.EffectiveHours < 0) attendance.EffectiveHours = 0;
        }
    }
}
