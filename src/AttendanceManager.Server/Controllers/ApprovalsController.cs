using AttendanceManager.Core.Interfaces;
using AttendanceManager.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceManager.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class ApprovalsController : ControllerBase
{
    private readonly ILeaveService _leaveService;
    private readonly ICorrectionService _correctionService;
    private readonly IEmployeeService _employeeService;

    public ApprovalsController(ILeaveService leaveService, ICorrectionService correctionService, IEmployeeService employeeService)
    {
        _leaveService = leaveService;
        _correctionService = correctionService;
        _employeeService = employeeService;
    }

    [HttpGet("pending")]
    public async Task<ActionResult<ApiResponse<List<ApprovalItemDto>>>> GetPending()
    {
        var items = new List<ApprovalItemDto>();

        var leaves = await _leaveService.GetPendingLeaveRequestsAsync();
        foreach (var l in leaves)
        {
            var employee = await _employeeService.GetByIdAsync(l.EmployeeId);
            items.Add(new ApprovalItemDto
            {
                Type = "Leave",
                Id = l.Id,
                EmployeeId = l.EmployeeId,
                EmployeeName = employee?.FullName ?? "",
                Summary = $"{l.StartDate:yyyy-MM-dd} to {l.EndDate:yyyy-MM-dd} ({l.TotalDays} day(s)) - {l.Reason}",
                RequestedAt = l.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }

        var corrections = await _correctionService.GetPendingAsync();
        foreach (var c in corrections)
        {
            var employee = await _employeeService.GetByIdAsync(c.EmployeeId);
            items.Add(new ApprovalItemDto
            {
                Type = "Correction",
                Id = c.Id,
                EmployeeId = c.EmployeeId,
                EmployeeName = employee?.FullName ?? "",
                Summary = $"{c.Date:yyyy-MM-dd} - {c.ClaimedStatus} - {c.Reason}",
                RequestedAt = c.RequestedAt.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }

        return Ok(ApiResponse<List<ApprovalItemDto>>.Ok(items.OrderBy(i => i.RequestedAt).ToList()));
    }
}
