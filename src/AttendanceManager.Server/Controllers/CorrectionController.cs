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
public class CorrectionController : ControllerBase
{
    private readonly ICorrectionService _correctionService;
    private readonly IEmployeeService _employeeService;

    public CorrectionController(ICorrectionService correctionService, IEmployeeService employeeService)
    {
        _correctionService = correctionService;
        _employeeService = employeeService;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CorrectionDto>>> Submit([FromBody] SubmitCorrectionRequest request)
    {
        var correction = new Correction
        {
            EmployeeId = request.EmployeeId,
            Date = DateOnly.Parse(request.Date),
            ClaimedStatus = Enum.Parse<ClaimedStatus>(request.ClaimedStatus),
            ClaimedLoginTime = string.IsNullOrEmpty(request.ClaimedLoginTime) ? null : DateTime.Parse(request.ClaimedLoginTime),
            ClaimedLogoutTime = string.IsNullOrEmpty(request.ClaimedLogoutTime) ? null : DateTime.Parse(request.ClaimedLogoutTime),
            Reason = request.Reason
        };

        try
        {
            var result = await _correctionService.SubmitAsync(correction);
            return Ok(ApiResponse<CorrectionDto>.Ok(await MapToDto(result)));
        }
        catch (InvalidOperationException ex)
        {
            return Ok(ApiResponse<CorrectionDto>.Fail(ex.Message));
        }
    }

    [HttpPost("{id}/approve")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse>> Approve(int id, [FromQuery] string? comment)
    {
        var adminId = int.Parse(User.FindFirst("EmployeeId")?.Value ?? "0");
        await _correctionService.ApproveAsync(id, adminId, comment);
        return Ok(ApiResponse.Ok("Correction approved."));
    }

    [HttpPost("{id}/reject")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse>> Reject(int id, [FromQuery] string? comment)
    {
        var adminId = int.Parse(User.FindFirst("EmployeeId")?.Value ?? "0");
        await _correctionService.RejectAsync(id, adminId, comment);
        return Ok(ApiResponse.Ok("Correction rejected."));
    }

    [HttpGet("employee/{employeeId}")]
    public async Task<ActionResult<ApiResponse<List<CorrectionDto>>>> GetByEmployee(int employeeId)
    {
        var corrections = await _correctionService.GetByEmployeeAsync(employeeId);
        var dtos = new List<CorrectionDto>();
        foreach (var c in corrections) dtos.Add(await MapToDto(c));
        return Ok(ApiResponse<List<CorrectionDto>>.Ok(dtos));
    }

    [HttpGet("pending")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<List<CorrectionDto>>>> GetPending()
    {
        var corrections = await _correctionService.GetPendingAsync();
        var dtos = new List<CorrectionDto>();
        foreach (var c in corrections) dtos.Add(await MapToDto(c));
        return Ok(ApiResponse<List<CorrectionDto>>.Ok(dtos));
    }

    private async Task<CorrectionDto> MapToDto(Correction c)
    {
        var employee = await _employeeService.GetByIdAsync(c.EmployeeId);
        var decidedBy = c.DecidedById.HasValue ? await _employeeService.GetByIdAsync(c.DecidedById.Value) : null;

        return new CorrectionDto
        {
            Id = c.Id,
            EmployeeId = c.EmployeeId,
            EmployeeName = employee?.FullName ?? "",
            Date = c.Date.ToString("yyyy-MM-dd"),
            ClaimedStatus = c.ClaimedStatus.ToString(),
            ClaimedLoginTime = c.ClaimedLoginTime?.ToString("yyyy-MM-dd HH:mm:ss"),
            ClaimedLogoutTime = c.ClaimedLogoutTime?.ToString("yyyy-MM-dd HH:mm:ss"),
            Reason = c.Reason,
            Status = c.Status.ToString(),
            AdminComment = c.AdminComment,
            RequestedAt = c.RequestedAt.ToString("yyyy-MM-dd HH:mm:ss"),
            DecidedAt = c.DecidedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
            DecidedByName = decidedBy?.FullName
        };
    }
}
