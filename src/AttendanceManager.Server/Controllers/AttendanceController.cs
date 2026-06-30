using System.Security.Claims;
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
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;
    private readonly IEmployeeService _employeeService;

    public AttendanceController(IAttendanceService attendanceService, IEmployeeService employeeService)
    {
        _attendanceService = attendanceService;
        _employeeService = employeeService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AttendanceDto>>> RecordLogin([FromBody] AgentLoginRequest request)
    {
        var attendance = await _attendanceService.RecordLoginAsync(
            request.EmployeeId, request.ComputerName, request.WindowsUsername, request.IpAddress);

        if (attendance == null)
            return Ok(ApiResponse<AttendanceDto>.Fail("Failed to record login."));

        return Ok(ApiResponse<AttendanceDto>.Ok(MapToDto(attendance)));
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AttendanceDto>>> RecordLogout([FromBody] AgentLogoutRequest request)
    {
        var attendance = await _attendanceService.RecordLogoutAsync(request.EmployeeId);
        if (attendance == null)
            return Ok(ApiResponse<AttendanceDto>.Fail("No attendance record found for today."));

        return Ok(ApiResponse<AttendanceDto>.Ok(MapToDto(attendance)));
    }

    [HttpPost("idle")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse>> RecordIdleTime([FromBody] IdleTimeDto request)
    {
        await _attendanceService.RecordIdleTimeAsync(request.EmployeeId, request.IdleStartTime, request.IdleEndTime);
        return Ok(ApiResponse.Ok("Idle time recorded."));
    }

    [HttpGet("today/{employeeId}")]
    public async Task<ActionResult<ApiResponse<AttendanceDto>>> GetTodayAttendance(int employeeId)
    {
        var attendance = await _attendanceService.GetTodayAttendanceAsync(employeeId);
        if (attendance == null)
            return Ok(ApiResponse<AttendanceDto>.Fail("No attendance found for today."));

        return Ok(ApiResponse<AttendanceDto>.Ok(MapToDto(attendance)));
    }

    [HttpGet("range/{employeeId}")]
    public async Task<ActionResult<ApiResponse<List<AttendanceDto>>>> GetAttendanceByRange(
        int employeeId, [FromQuery] string startDate, [FromQuery] string endDate)
    {
        var start = DateOnly.Parse(startDate);
        var end = DateOnly.Parse(endDate);
        var records = await _attendanceService.GetAttendanceByDateRangeAsync(employeeId, start, end);
        return Ok(ApiResponse<List<AttendanceDto>>.Ok(records.Select(MapToDto).ToList()));
    }

    [HttpGet("daily")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<List<AttendanceDto>>>> GetDailyAttendance([FromQuery] string date)
    {
        var d = DateOnly.Parse(date);
        var records = await _attendanceService.GetAllAttendanceByDateAsync(d);
        var dtos = new List<AttendanceDto>();
        foreach (var r in records)
        {
            var dto = MapToDto(r);
            var emp = await _employeeService.GetByIdAsync(r.EmployeeId);
            if (emp != null) dto.EmployeeName = emp.FullName;
            dtos.Add(dto);
        }
        return Ok(ApiResponse<List<AttendanceDto>>.Ok(dtos));
    }

    [HttpPut("{id}/status")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse>> UpdateStatus(long id, [FromQuery] AttendanceStatus status, [FromQuery] string reason)
    {
        var adminId = GetCurrentEmployeeId();
        await _attendanceService.UpdateAttendanceStatusAsync(id, status, adminId, reason);
        return Ok(ApiResponse.Ok("Status updated."));
    }

    [HttpPut("{id}/login-time")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse>> UpdateLoginTime(long id, [FromQuery] DateTime loginTime, [FromQuery] string reason)
    {
        var adminId = GetCurrentEmployeeId();
        await _attendanceService.UpdateLoginTimeAsync(id, loginTime, adminId, reason);
        return Ok(ApiResponse.Ok("Login time updated."));
    }

    [HttpPut("{id}/logout-time")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse>> UpdateLogoutTime(long id, [FromQuery] DateTime logoutTime, [FromQuery] string reason)
    {
        var adminId = GetCurrentEmployeeId();
        await _attendanceService.UpdateLogoutTimeAsync(id, logoutTime, adminId, reason);
        return Ok(ApiResponse.Ok("Logout time updated."));
    }

    [HttpPost("manual")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse>> InsertManualAttendance([FromBody] AttendanceDto dto, [FromQuery] string reason)
    {
        var adminId = GetCurrentEmployeeId();
        var attendance = new Attendance
        {
            EmployeeId = dto.EmployeeId,
            Date = DateOnly.Parse(dto.Date),
            LoginTime = string.IsNullOrEmpty(dto.LoginTime) ? null : DateTime.Parse(dto.LoginTime),
            LogoutTime = string.IsNullOrEmpty(dto.LogoutTime) ? null : DateTime.Parse(dto.LogoutTime),
            Status = Enum.Parse<AttendanceStatus>(dto.Status),
            Remarks = dto.Remarks
        };
        await _attendanceService.InsertManualAttendanceAsync(attendance, adminId, reason);
        return Ok(ApiResponse.Ok("Manual attendance inserted."));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse>> DeleteAttendance(long id, [FromQuery] string reason)
    {
        var adminId = GetCurrentEmployeeId();
        await _attendanceService.DeleteAttendanceAsync(id, adminId, reason);
        return Ok(ApiResponse.Ok("Attendance deleted."));
    }

    [HttpPost("process-daily")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse>> ProcessDailyStatus([FromQuery] string date)
    {
        var d = DateOnly.Parse(date);
        await _attendanceService.ProcessDailyAttendanceStatusAsync(d);
        return Ok(ApiResponse.Ok("Daily attendance processed."));
    }

    private int GetCurrentEmployeeId()
    {
        var claim = User.FindFirst("EmployeeId");
        return claim != null ? int.Parse(claim.Value) : 0;
    }

    private static AttendanceDto MapToDto(Attendance a) => new()
    {
        Id = a.Id,
        EmployeeId = a.EmployeeId,
        Date = a.Date.ToString("yyyy-MM-dd"),
        LoginTime = a.LoginTime?.ToString("yyyy-MM-dd HH:mm:ss"),
        LogoutTime = a.LogoutTime?.ToString("yyyy-MM-dd HH:mm:ss"),
        ComputerName = a.ComputerName,
        WindowsUsername = a.WindowsUsername,
        IpAddress = a.IpAddress,
        Status = a.Status.ToString(),
        TotalHours = a.TotalHours,
        IdleTimeMinutes = a.IdleTimeMinutes,
        EffectiveHours = a.EffectiveHours,
        IsManualEntry = a.IsManualEntry,
        Remarks = a.Remarks
    };
}
