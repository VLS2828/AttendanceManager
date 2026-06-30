using AttendanceManager.Core.Interfaces;
using AttendanceManager.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceManager.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class AuditController : ControllerBase
{
    private readonly IAuditService _auditService;

    public AuditController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<object>>>> GetAuditLogs(
        [FromQuery] int? employeeId, [FromQuery] string? startDate, [FromQuery] string? endDate)
    {
        var start = startDate != null ? DateOnly.Parse(startDate) : (DateOnly?)null;
        var end = endDate != null ? DateOnly.Parse(endDate) : (DateOnly?)null;

        var logs = await _auditService.GetAuditLogsAsync(employeeId, start, end);
        return Ok(ApiResponse<List<object>>.Ok(logs.Select(l => (object)new
        {
            l.Id, l.EmployeeId, Date = l.Date.ToString("yyyy-MM-dd"),
            l.TableName, l.RecordId, l.FieldName, l.PreviousValue, l.NewValue,
            l.AdminId, l.AdminName, l.Reason, Timestamp = l.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")
        }).ToList()));
    }
}
