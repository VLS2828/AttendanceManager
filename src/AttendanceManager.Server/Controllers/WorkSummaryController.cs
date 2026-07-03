using AttendanceManager.Core.Entities;
using AttendanceManager.Core.Interfaces;
using AttendanceManager.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceManager.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WorkSummaryController : ControllerBase
{
    private readonly IWorkSummaryService _workSummaryService;

    public WorkSummaryController(IWorkSummaryService workSummaryService)
    {
        _workSummaryService = workSummaryService;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<WorkSummaryDto>>> Submit([FromBody] SubmitWorkSummaryRequest request)
    {
        if (request.Entries.Count == 0)
            return Ok(ApiResponse<WorkSummaryDto>.Fail("At least one entry is required."));

        var entries = request.Entries.Select(e => new WorkSummaryEntry
        {
            FromTime = TimeOnly.Parse(e.FromTime),
            ToTime = TimeOnly.Parse(e.ToTime),
            Client = e.Client,
            Description = e.Description
        }).ToList();

        var result = await _workSummaryService.SubmitAsync(request.EmployeeId, DateOnly.Parse(request.Date), entries);
        return Ok(ApiResponse<WorkSummaryDto>.Ok(MapToDto(result)));
    }

    [HttpGet("{employeeId}/{date}")]
    public async Task<ActionResult<ApiResponse<WorkSummaryDto>>> GetByDate(int employeeId, string date)
    {
        var summary = await _workSummaryService.GetByDateAsync(employeeId, DateOnly.Parse(date));
        if (summary == null) return Ok(ApiResponse<WorkSummaryDto>.Fail("No work summary found."));
        return Ok(ApiResponse<WorkSummaryDto>.Ok(MapToDto(summary)));
    }

    [HttpGet("{employeeId}/range")]
    public async Task<ActionResult<ApiResponse<List<WorkSummaryDto>>>> GetByRange(
        int employeeId, [FromQuery] string startDate, [FromQuery] string endDate)
    {
        var summaries = await _workSummaryService.GetByRangeAsync(
            employeeId, DateOnly.Parse(startDate), DateOnly.Parse(endDate));
        return Ok(ApiResponse<List<WorkSummaryDto>>.Ok(summaries.Select(MapToDto).ToList()));
    }

    private static WorkSummaryDto MapToDto(WorkSummary w) => new()
    {
        Id = w.Id,
        EmployeeId = w.EmployeeId,
        Date = w.Date.ToString("yyyy-MM-dd"),
        Entries = w.Entries.Select(e => new WorkSummaryEntryDto
        {
            FromTime = e.FromTime.ToString("HH:mm"),
            ToTime = e.ToTime.ToString("HH:mm"),
            Client = e.Client,
            Description = e.Description
        }).ToList()
    };
}
