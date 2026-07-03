using AttendanceManager.Core.Entities;
using AttendanceManager.Core.Interfaces;
using AttendanceManager.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceManager.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeaveController : ControllerBase
{
    private readonly ILeaveService _leaveService;
    private readonly IEmployeeService _employeeService;

    public LeaveController(ILeaveService leaveService, IEmployeeService employeeService)
    {
        _leaveService = leaveService;
        _employeeService = employeeService;
    }

    [HttpPost("request")]
    public async Task<ActionResult<ApiResponse<LeaveRequestDto>>> SubmitLeaveRequest([FromBody] SubmitLeaveRequest request)
    {
        var leaveRequest = new LeaveRequest
        {
            EmployeeId = request.EmployeeId,
            LeaveTypeId = request.LeaveTypeId,
            StartDate = DateOnly.Parse(request.StartDate),
            EndDate = DateOnly.Parse(request.EndDate),
            Reason = request.Reason
        };

        var result = await _leaveService.SubmitLeaveRequestAsync(leaveRequest);
        return Ok(ApiResponse<LeaveRequestDto>.Ok(await MapToDto(result)));
    }

    [HttpPost("{id}/approve")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse>> Approve(int id, [FromQuery] string? remarks)
    {
        var adminId = int.Parse(User.FindFirst("EmployeeId")?.Value ?? "0");
        await _leaveService.ApproveLeaveAsync(id, adminId, remarks);
        return Ok(ApiResponse.Ok("Leave approved."));
    }

    [HttpPost("{id}/reject")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse>> Reject(int id, [FromQuery] string? remarks)
    {
        var adminId = int.Parse(User.FindFirst("EmployeeId")?.Value ?? "0");
        await _leaveService.RejectLeaveAsync(id, adminId, remarks);
        return Ok(ApiResponse.Ok("Leave rejected."));
    }

    [HttpPost("{id}/cancel")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse>> Cancel(int id, [FromQuery] string? remarks)
    {
        var adminId = int.Parse(User.FindFirst("EmployeeId")?.Value ?? "0");
        await _leaveService.CancelLeaveAsync(id, adminId, remarks);
        return Ok(ApiResponse.Ok("Leave cancelled."));
    }

    [HttpGet("employee/{employeeId}")]
    public async Task<ActionResult<ApiResponse<List<LeaveRequestDto>>>> GetByEmployee(int employeeId)
    {
        var requests = await _leaveService.GetLeaveRequestsByEmployeeAsync(employeeId);
        var dtos = new List<LeaveRequestDto>();
        foreach (var r in requests)
            dtos.Add(await MapToDto(r));
        return Ok(ApiResponse<List<LeaveRequestDto>>.Ok(dtos));
    }

    [HttpGet("pending")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<List<LeaveRequestDto>>>> GetPending()
    {
        var requests = await _leaveService.GetPendingLeaveRequestsAsync();
        var dtos = new List<LeaveRequestDto>();
        foreach (var r in requests)
            dtos.Add(await MapToDto(r));
        return Ok(ApiResponse<List<LeaveRequestDto>>.Ok(dtos));
    }

    [HttpGet("types")]
    public async Task<ActionResult<ApiResponse<List<LeaveTypeDto>>>> GetLeaveTypes()
    {
        var types = await _leaveService.GetLeaveTypesAsync();
        return Ok(ApiResponse<List<LeaveTypeDto>>.Ok(types.Select(t => new LeaveTypeDto
        {
            Id = t.Id,
            Name = t.Name,
            DefaultDaysPerYear = t.DefaultDaysPerYear
        }).ToList()));
    }

    [HttpGet("balance/{employeeId}")]
    public async Task<ActionResult<ApiResponse<List<LeaveBalanceDto>>>> GetBalance(int employeeId, [FromQuery] int? year)
    {
        var y = year ?? DateTime.Now.Year;
        var balances = await _leaveService.GetLeaveBalancesAsync(employeeId, y);
        return Ok(ApiResponse<List<LeaveBalanceDto>>.Ok(
            balances.Select(b => new LeaveBalanceDto
            {
                LeaveTypeName = b.LeaveType?.Name ?? "",
                TotalDays = b.TotalDays,
                UsedDays = b.UsedDays,
                RemainingDays = b.TotalDays - b.UsedDays
            }).ToList()));
    }

    private async Task<LeaveRequestDto> MapToDto(LeaveRequest r)
    {
        var employee = await _employeeService.GetByIdAsync(r.EmployeeId);
        Employee? approvedBy = r.ApprovedById.HasValue ? await _employeeService.GetByIdAsync(r.ApprovedById.Value) : null;

        return new LeaveRequestDto
        {
            Id = r.Id,
            EmployeeId = r.EmployeeId,
            EmployeeName = employee?.FullName ?? "",
            LeaveTypeId = r.LeaveTypeId,
            LeaveTypeName = r.LeaveType?.Name ?? "",
            StartDate = r.StartDate.ToString("yyyy-MM-dd"),
            EndDate = r.EndDate.ToString("yyyy-MM-dd"),
            TotalDays = r.TotalDays,
            Reason = r.Reason,
            Status = r.Status.ToString(),
            ApprovedByName = approvedBy?.FullName,
            ApprovedAt = r.ApprovedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
            AdminRemarks = r.AdminRemarks,
            CreatedAt = r.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
        };
    }
}

