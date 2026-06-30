using AttendanceManager.Core.Interfaces;
using AttendanceManager.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceManager.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class SettingsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public SettingsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<object>>>> GetAll()
    {
        var settings = await _unitOfWork.AppSettings.GetAllAsync();
        return Ok(ApiResponse<List<object>>.Ok(settings.Select(s => (object)new
        {
            s.Id, s.Key, s.Value, s.Description
        }).ToList()));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse>> Update(int id, [FromBody] UpdateSettingRequest request)
    {
        var setting = await _unitOfWork.AppSettings.GetByIdAsync(id);
        if (setting == null) return Ok(ApiResponse.Fail("Setting not found."));

        setting.Value = request.Value;
        setting.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.AppSettings.Update(setting);
        await _unitOfWork.SaveChangesAsync();
        return Ok(ApiResponse.Ok("Setting updated."));
    }
}

public class UpdateSettingRequest
{
    public string Value { get; set; } = string.Empty;
}
