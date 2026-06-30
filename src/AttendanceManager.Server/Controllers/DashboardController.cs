using AttendanceManager.Core.Entities;
using AttendanceManager.Core.Enums;
using AttendanceManager.Core.Interfaces;
using AttendanceManager.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceManager.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;
    private readonly IEmployeeService _employeeService;
    private readonly ILeaveService _leaveService;
    private readonly IReportService _reportService;
    private readonly IUnitOfWork _unitOfWork;

    public DashboardController(
        IAttendanceService attendanceService,
        IEmployeeService employeeService,
        ILeaveService leaveService,
        IReportService reportService,
        IUnitOfWork unitOfWork)
    {
        _attendanceService = attendanceService;
        _employeeService = employeeService;
        _leaveService = leaveService;
        _reportService = reportService;
        _unitOfWork = unitOfWork;
    }

    [HttpGet("admin")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<AdminDashboardDto>>> GetAdminDashboard()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var allEmployees = await _employeeService.GetAllActiveAsync();
        var todayAttendance = (await _attendanceService.GetAllAttendanceByDateAsync(today)).ToList();

        var workStartSetting = await _unitOfWork.AppSettings.FirstOrDefaultAsync(s => s.Key == "WorkStartTime");
        var lateThresholdSetting = await _unitOfWork.AppSettings.FirstOrDefaultAsync(s => s.Key == "LateThresholdMinutes");
        var workEndSetting = await _unitOfWork.AppSettings.FirstOrDefaultAsync(s => s.Key == "WorkEndTime");

        var workStart = TimeOnly.Parse(workStartSetting?.Value ?? "09:30");
        var lateMinutes = int.Parse(lateThresholdSetting?.Value ?? "15");
        var workEnd = TimeOnly.Parse(workEndSetting?.Value ?? "18:30");
        var lateThreshold = workStart.AddMinutes(lateMinutes);

        var presentRecords = todayAttendance.Where(a => a.Status == AttendanceStatus.Present).ToList();
        var onlineCount = presentRecords.Count(a => a.LogoutTime == null);

        var holidays = await _unitOfWork.Holidays.FindAsync(h =>
            h.Date.Month == today.Month && h.Date.Year == today.Year);

        var dashboard = new AdminDashboardDto
        {
            TotalEmployees = allEmployees.Count(),
            EmployeesOnline = onlineCount,
            EmployeesOffline = allEmployees.Count() - onlineCount,
            PresentToday = presentRecords.Count,
            AbsentToday = todayAttendance.Count(a => a.Status == AttendanceStatus.Absent),
            OnLeaveToday = todayAttendance.Count(a => a.Status == AttendanceStatus.Leave),
            LateArrivals = presentRecords.Count(a =>
                a.LoginTime.HasValue && TimeOnly.FromDateTime(a.LoginTime.Value) > lateThreshold),
            EarlyDepartures = presentRecords.Count(a =>
                a.LogoutTime.HasValue && TimeOnly.FromDateTime(a.LogoutTime.Value) < workEnd),
            HolidaysThisMonth = holidays.Count(),
            TodayAttendance = todayAttendance.Select(a =>
            {
                var emp = allEmployees.FirstOrDefault(e => e.Id == a.EmployeeId);
                return new AttendanceDto
                {
                    Id = a.Id,
                    EmployeeId = a.EmployeeId,
                    EmployeeName = emp?.FullName ?? "",
                    Date = a.Date.ToString("yyyy-MM-dd"),
                    LoginTime = a.LoginTime?.ToString("HH:mm:ss"),
                    LogoutTime = a.LogoutTime?.ToString("HH:mm:ss"),
                    Status = a.Status.ToString(),
                    TotalHours = a.TotalHours,
                    IdleTimeMinutes = a.IdleTimeMinutes,
                    EffectiveHours = a.EffectiveHours
                };
            }).ToList()
        };

        return Ok(ApiResponse<AdminDashboardDto>.Ok(dashboard));
    }

    [HttpGet("employee/{employeeId}")]
    public async Task<ActionResult<ApiResponse<EmployeeDashboardDto>>> GetEmployeeDashboard(int employeeId)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var startOfMonth = new DateOnly(today.Year, today.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

        var todayAttendance = await _attendanceService.GetTodayAttendanceAsync(employeeId);
        var monthlyAttendance = (await _attendanceService.GetAttendanceByDateRangeAsync(employeeId, startOfMonth, endOfMonth)).ToList();
        var leaveBalances = (await _leaveService.GetLeaveBalancesAsync(employeeId, today.Year)).ToList();

        var dashboard = new EmployeeDashboardDto
        {
            TodayAttendance = todayAttendance != null ? new AttendanceDto
            {
                Id = todayAttendance.Id,
                Date = todayAttendance.Date.ToString("yyyy-MM-dd"),
                LoginTime = todayAttendance.LoginTime?.ToString("HH:mm:ss"),
                LogoutTime = todayAttendance.LogoutTime?.ToString("HH:mm:ss"),
                Status = todayAttendance.Status.ToString(),
                TotalHours = todayAttendance.TotalHours,
                IdleTimeMinutes = todayAttendance.IdleTimeMinutes,
                EffectiveHours = todayAttendance.EffectiveHours
            } : null,
            LeaveBalances = leaveBalances.Select(b => new LeaveBalanceDto
            {
                LeaveTypeName = b.LeaveType?.Name ?? "",
                TotalDays = b.TotalDays,
                UsedDays = b.UsedDays,
                RemainingDays = b.TotalDays - b.UsedDays
            }).ToList(),
            MonthlyAttendance = monthlyAttendance.Select(a => new AttendanceDto
            {
                Id = a.Id,
                Date = a.Date.ToString("yyyy-MM-dd"),
                LoginTime = a.LoginTime?.ToString("HH:mm:ss"),
                LogoutTime = a.LogoutTime?.ToString("HH:mm:ss"),
                Status = a.Status.ToString(),
                TotalHours = a.TotalHours,
                IdleTimeMinutes = a.IdleTimeMinutes,
                EffectiveHours = a.EffectiveHours
            }).ToList(),
            MonthlySummary = new MonthlySummaryDto
            {
                PresentDays = monthlyAttendance.Count(a => a.Status == AttendanceStatus.Present),
                AbsentDays = monthlyAttendance.Count(a => a.Status == AttendanceStatus.Absent),
                LeaveDays = monthlyAttendance.Count(a => a.Status == AttendanceStatus.Leave),
                HalfDays = monthlyAttendance.Count(a => a.Status == AttendanceStatus.HalfDay),
                Holidays = monthlyAttendance.Count(a => a.Status == AttendanceStatus.Holiday),
                WeeklyOffs = monthlyAttendance.Count(a => a.Status == AttendanceStatus.WeeklyOff),
                TotalWorkingHours = Math.Round(monthlyAttendance.Sum(a => a.TotalHours), 2),
                TotalIdleMinutes = Math.Round(monthlyAttendance.Sum(a => a.IdleTimeMinutes), 2),
                TotalEffectiveHours = Math.Round(monthlyAttendance.Sum(a => a.EffectiveHours), 2),
                AverageWorkingHours = monthlyAttendance.Where(a => a.TotalHours > 0).Any()
                    ? Math.Round(monthlyAttendance.Where(a => a.TotalHours > 0).Average(a => a.TotalHours), 2)
                    : 0
            }
        };

        return Ok(ApiResponse<EmployeeDashboardDto>.Ok(dashboard));
    }
}
