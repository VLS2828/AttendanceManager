using AttendanceManager.Core.Entities;
using AttendanceManager.Core.Interfaces;
using AttendanceManager.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceManager.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HolidayController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public HolidayController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<object>>>> GetAll([FromQuery] int? year)
    {
        var y = year ?? DateTime.Now.Year;
        var holidays = await _unitOfWork.Holidays.FindAsync(h => h.Year == y);
        return Ok(ApiResponse<List<object>>.Ok(holidays.Select(h => (object)new
        {
            h.Id, h.Name, Date = h.Date.ToString("yyyy-MM-dd"), h.Description, h.IsOptional, h.Year
        }).ToList()));
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse>> Create([FromBody] CreateHolidayRequest request)
    {
        var date = DateOnly.Parse(request.Date);
        var holiday = new Holiday
        {
            Name = request.Name,
            Date = date,
            Description = request.Description,
            IsOptional = request.IsOptional,
            Year = date.Year
        };
        await _unitOfWork.Holidays.AddAsync(holiday);
        await _unitOfWork.SaveChangesAsync();
        return Ok(ApiResponse.Ok("Holiday created."));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse>> Delete(int id)
    {
        var holiday = await _unitOfWork.Holidays.GetByIdAsync(id);
        if (holiday == null) return Ok(ApiResponse.Fail("Holiday not found."));
        _unitOfWork.Holidays.Remove(holiday);
        await _unitOfWork.SaveChangesAsync();
        return Ok(ApiResponse.Ok("Holiday deleted."));
    }
}

public class CreateHolidayRequest
{
    public string Name { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsOptional { get; set; }
}
