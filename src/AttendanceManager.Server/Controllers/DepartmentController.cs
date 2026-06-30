using AttendanceManager.Core.Entities;
using AttendanceManager.Core.Interfaces;
using AttendanceManager.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceManager.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DepartmentController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public DepartmentController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<object>>>> GetAll()
    {
        var departments = await _unitOfWork.Departments.FindAsync(d => d.IsActive);
        return Ok(ApiResponse<List<object>>.Ok(departments.Select(d => (object)new
        {
            d.Id, d.Name, d.Description
        }).ToList()));
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse>> Create([FromBody] CreateDepartmentRequest request)
    {
        var dept = new Department
        {
            Name = request.Name,
            Description = request.Description
        };
        await _unitOfWork.Departments.AddAsync(dept);
        await _unitOfWork.SaveChangesAsync();
        return Ok(ApiResponse.Ok("Department created."));
    }
}

public class CreateDepartmentRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
