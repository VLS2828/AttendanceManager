using AttendanceManager.Core.Interfaces;
using AttendanceManager.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceManager.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class ReportController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly IEmployeeService _employeeService;

    public ReportController(IReportService reportService, IEmployeeService employeeService)
    {
        _reportService = reportService;
        _employeeService = employeeService;
    }

    [HttpGet("daily")]
    public async Task<ActionResult<ApiResponse<List<AttendanceDto>>>> DailyReport([FromQuery] string date)
    {
        if (!DateOnly.TryParse(date, out var d))
            return BadRequest(ApiResponse<List<AttendanceDto>>.Fail("Invalid date format. Use yyyy-MM-dd."));
        var records = await _reportService.GetDailyAttendanceReportAsync(d);
        var dtos = new List<AttendanceDto>();
        foreach (var r in records)
        {
            var emp = await _employeeService.GetByIdAsync(r.EmployeeId);
            dtos.Add(new AttendanceDto
            {
                Id = r.Id, EmployeeId = r.EmployeeId, EmployeeName = emp?.FullName ?? "",
                Date = r.Date.ToString("yyyy-MM-dd"), LoginTime = r.LoginTime?.ToString("HH:mm:ss"),
                LogoutTime = r.LogoutTime?.ToString("HH:mm:ss"), Status = r.Status.ToString(),
                TotalHours = r.TotalHours, IdleTimeMinutes = r.IdleTimeMinutes, EffectiveHours = r.EffectiveHours
            });
        }
        return Ok(ApiResponse<List<AttendanceDto>>.Ok(dtos));
    }

    [HttpGet("monthly/{employeeId}")]
    public async Task<ActionResult<ApiResponse<List<AttendanceDto>>>> MonthlyReport(int employeeId, [FromQuery] int year, [FromQuery] int month)
    {
        var records = await _reportService.GetMonthlyAttendanceReportAsync(employeeId, year, month);
        return Ok(ApiResponse<List<AttendanceDto>>.Ok(records.Select(r => new AttendanceDto
        {
            Id = r.Id, EmployeeId = r.EmployeeId, Date = r.Date.ToString("yyyy-MM-dd"),
            LoginTime = r.LoginTime?.ToString("HH:mm:ss"), LogoutTime = r.LogoutTime?.ToString("HH:mm:ss"),
            Status = r.Status.ToString(), TotalHours = r.TotalHours, IdleTimeMinutes = r.IdleTimeMinutes,
            EffectiveHours = r.EffectiveHours
        }).ToList()));
    }

    [HttpGet("late-arrivals")]
    public async Task<ActionResult<ApiResponse<List<AttendanceDto>>>> LateArrivals(
        [FromQuery] string startDate, [FromQuery] string endDate, [FromQuery] string? threshold)
    {
        if (!DateOnly.TryParse(startDate, out var start) || !DateOnly.TryParse(endDate, out var end))
            return BadRequest(ApiResponse<List<AttendanceDto>>.Fail("Invalid date format. Use yyyy-MM-dd."));
        if (!TimeOnly.TryParse(threshold ?? "09:45", out var thresholdTime))
            return BadRequest(ApiResponse<List<AttendanceDto>>.Fail("Invalid threshold time format."));
        var records = await _reportService.GetLateArrivalsReportAsync(start, end, thresholdTime);

        var dtos = new List<AttendanceDto>();
        foreach (var r in records)
        {
            var emp = await _employeeService.GetByIdAsync(r.EmployeeId);
            dtos.Add(new AttendanceDto
            {
                Id = r.Id, EmployeeId = r.EmployeeId, EmployeeName = emp?.FullName ?? "",
                Date = r.Date.ToString("yyyy-MM-dd"), LoginTime = r.LoginTime?.ToString("HH:mm:ss"),
                Status = r.Status.ToString(), TotalHours = r.TotalHours
            });
        }
        return Ok(ApiResponse<List<AttendanceDto>>.Ok(dtos));
    }

    [HttpGet("overtime")]
    public async Task<ActionResult<ApiResponse<List<AttendanceDto>>>> Overtime(
        [FromQuery] string startDate, [FromQuery] string endDate, [FromQuery] double? standardHours)
    {
        if (!DateOnly.TryParse(startDate, out var start) || !DateOnly.TryParse(endDate, out var end))
            return BadRequest(ApiResponse<List<AttendanceDto>>.Fail("Invalid date format. Use yyyy-MM-dd."));
        var records = await _reportService.GetOvertimeReportAsync(start, end, standardHours ?? 9);

        var dtos = new List<AttendanceDto>();
        foreach (var r in records)
        {
            var emp = await _employeeService.GetByIdAsync(r.EmployeeId);
            dtos.Add(new AttendanceDto
            {
                Id = r.Id, EmployeeId = r.EmployeeId, EmployeeName = emp?.FullName ?? "",
                Date = r.Date.ToString("yyyy-MM-dd"), TotalHours = r.TotalHours, EffectiveHours = r.EffectiveHours
            });
        }
        return Ok(ApiResponse<List<AttendanceDto>>.Ok(dtos));
    }

    [HttpGet("idle-time")]
    public async Task<ActionResult<ApiResponse<List<AttendanceDto>>>> IdleTime(
        [FromQuery] string startDate, [FromQuery] string endDate)
    {
        if (!DateOnly.TryParse(startDate, out var start) || !DateOnly.TryParse(endDate, out var end))
            return BadRequest(ApiResponse<List<AttendanceDto>>.Fail("Invalid date format. Use yyyy-MM-dd."));
        var records = await _reportService.GetIdleTimeReportAsync(start, end);

        var dtos = new List<AttendanceDto>();
        foreach (var r in records)
        {
            var emp = await _employeeService.GetByIdAsync(r.EmployeeId);
            dtos.Add(new AttendanceDto
            {
                Id = r.Id, EmployeeId = r.EmployeeId, EmployeeName = emp?.FullName ?? "",
                Date = r.Date.ToString("yyyy-MM-dd"), IdleTimeMinutes = r.IdleTimeMinutes,
                TotalHours = r.TotalHours, EffectiveHours = r.EffectiveHours
            });
        }
        return Ok(ApiResponse<List<AttendanceDto>>.Ok(dtos));
    }

    [HttpGet("leave-summary")]
    public async Task<ActionResult<ApiResponse<List<LeaveRequestDto>>>> LeaveSummary(
        [FromQuery] string startDate, [FromQuery] string endDate)
    {
        if (!DateOnly.TryParse(startDate, out var start) || !DateOnly.TryParse(endDate, out var end))
            return BadRequest(ApiResponse<List<LeaveRequestDto>>.Fail("Invalid date format. Use yyyy-MM-dd."));
        var records = await _reportService.GetLeaveSummaryReportAsync(start, end);

        var dtos = new List<LeaveRequestDto>();
        foreach (var r in records)
        {
            var emp = await _employeeService.GetByIdAsync(r.EmployeeId);
            dtos.Add(new LeaveRequestDto
            {
                Id = r.Id, EmployeeId = r.EmployeeId, EmployeeName = emp?.FullName ?? "",
                StartDate = r.StartDate.ToString("yyyy-MM-dd"), EndDate = r.EndDate.ToString("yyyy-MM-dd"),
                TotalDays = r.TotalDays, Status = r.Status.ToString(), Reason = r.Reason
            });
        }
        return Ok(ApiResponse<List<LeaveRequestDto>>.Ok(dtos));
    }

    [HttpGet("holidays")]
    public async Task<ActionResult<ApiResponse<object>>> HolidaySummary([FromQuery] int year)
    {
        var holidays = await _reportService.GetHolidaySummaryReportAsync(year);
        return Ok(ApiResponse<object>.Ok(holidays.Select(h => new
        {
            h.Id, h.Name, Date = h.Date.ToString("yyyy-MM-dd"), h.Description, h.IsOptional
        }).ToList()));
    }

    [HttpGet("working-hours/{employeeId}")]
    public async Task<ActionResult<ApiResponse<List<AttendanceDto>>>> WorkingHours(
        int employeeId, [FromQuery] string startDate, [FromQuery] string endDate)
    {
        if (!DateOnly.TryParse(startDate, out var start) || !DateOnly.TryParse(endDate, out var end))
            return BadRequest(ApiResponse<List<AttendanceDto>>.Fail("Invalid date format. Use yyyy-MM-dd."));
        var records = await _reportService.GetEmployeeWorkingHoursReportAsync(employeeId, start, end);
        return Ok(ApiResponse<List<AttendanceDto>>.Ok(records.Select(r => new AttendanceDto
        {
            Id = r.Id, Date = r.Date.ToString("yyyy-MM-dd"), LoginTime = r.LoginTime?.ToString("HH:mm:ss"),
            LogoutTime = r.LogoutTime?.ToString("HH:mm:ss"), TotalHours = r.TotalHours,
            IdleTimeMinutes = r.IdleTimeMinutes, EffectiveHours = r.EffectiveHours, Status = r.Status.ToString()
        }).ToList()));
    }

    [HttpGet("department-summary")]
    public async Task<ActionResult<ApiResponse<Dictionary<string, object>>>> DepartmentSummary(
        [FromQuery] string startDate, [FromQuery] string endDate)
    {
        if (!DateOnly.TryParse(startDate, out var start) || !DateOnly.TryParse(endDate, out var end))
            return BadRequest(ApiResponse<Dictionary<string, object>>.Fail("Invalid date format. Use yyyy-MM-dd."));
        var summary = await _reportService.GetDepartmentSummaryAsync(start, end);
        return Ok(ApiResponse<Dictionary<string, object>>.Ok(summary));
    }
}
