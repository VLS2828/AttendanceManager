using AttendanceManager.Core.Entities;
using AttendanceManager.Core.Enums;
using AttendanceManager.Core.Interfaces;
using AttendanceManager.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceManager.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ActivitySessionController : ControllerBase
{
    private readonly IActivitySessionService _activitySessionService;

    public ActivitySessionController(IActivitySessionService activitySessionService)
    {
        _activitySessionService = activitySessionService;
    }

    [HttpPost("sync")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse>> Sync([FromBody] ActivitySessionSyncRequest request)
    {
        var intervals = request.Intervals.Select(i => new ActivitySession
        {
            IntervalStart = DateTime.Parse(i.Start),
            IntervalEnd = DateTime.Parse(i.End),
            State = Enum.Parse<ActivityState>(i.State)
        }).ToList();

        await _activitySessionService.SyncAsync(request.EmployeeId, intervals);
        return Ok(ApiResponse.Ok("Activity intervals synced."));
    }
}
