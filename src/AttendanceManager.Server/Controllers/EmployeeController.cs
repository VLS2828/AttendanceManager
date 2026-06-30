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
public class EmployeeController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly ILeaveService _leaveService;

    public EmployeeController(IEmployeeService employeeService, ILeaveService leaveService)
    {
        _employeeService = employeeService;
        _leaveService = leaveService;
    }

    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<List<EmployeeDto>>>> GetAll()
    {
        var employees = await _employeeService.GetAllActiveAsync();
        return Ok(ApiResponse<List<EmployeeDto>>.Ok(
            employees.Select(MapToDto).ToList()));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> GetById(int id)
    {
        var employee = await _employeeService.GetByIdAsync(id);
        if (employee == null) return Ok(ApiResponse<EmployeeDto>.Fail("Employee not found."));
        return Ok(ApiResponse<EmployeeDto>.Ok(MapToDto(employee)));
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> Create([FromBody] CreateEmployeeRequest request)
    {
        var employee = new Employee
        {
            EmployeeCode = request.EmployeeCode,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            DepartmentId = request.DepartmentId,
            Role = Enum.Parse<UserRole>(request.Role),
            JoiningDate = DateTime.Parse(request.JoiningDate)
        };

        var created = await _employeeService.CreateAsync(employee, request.Password);
        await _leaveService.InitializeLeaveBalancesAsync(created.Id, DateTime.Now.Year);
        return Ok(ApiResponse<EmployeeDto>.Ok(MapToDto(created), "Employee created successfully."));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse>> Update(int id, [FromBody] UpdateEmployeeRequest request)
    {
        var employee = await _employeeService.GetByIdAsync(id);
        if (employee == null) return Ok(ApiResponse.Fail("Employee not found."));

        employee.FirstName = request.FirstName;
        employee.LastName = request.LastName;
        employee.Email = request.Email;
        employee.Phone = request.Phone;
        employee.DepartmentId = request.DepartmentId;
        employee.Role = Enum.Parse<UserRole>(request.Role);

        await _employeeService.UpdateAsync(employee);
        return Ok(ApiResponse.Ok("Employee updated."));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse>> Deactivate(int id)
    {
        await _employeeService.DeactivateAsync(id);
        return Ok(ApiResponse.Ok("Employee deactivated."));
    }

    [HttpPost("{id}/change-password")]
    public async Task<ActionResult<ApiResponse>> ChangePassword(int id, [FromBody] ChangePasswordRequest request)
    {
        await _employeeService.ChangePasswordAsync(id, request.CurrentPassword, request.NewPassword);
        return Ok(ApiResponse.Ok("Password changed."));
    }

    [HttpPost("{id}/reset-password")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ApiResponse>> ResetPassword(int id, [FromBody] ResetPasswordRequest request)
    {
        var adminId = int.Parse(User.FindFirst("EmployeeId")?.Value ?? "0");
        await _employeeService.ResetPasswordAsync(id, request.NewPassword, adminId);
        return Ok(ApiResponse.Ok("Password reset."));
    }

    private static EmployeeDto MapToDto(Employee e) => new()
    {
        Id = e.Id,
        EmployeeCode = e.EmployeeCode,
        FirstName = e.FirstName,
        LastName = e.LastName,
        FullName = e.FullName,
        Email = e.Email,
        Phone = e.Phone,
        DepartmentId = e.DepartmentId,
        Department = e.Department?.Name ?? "",
        Role = e.Role.ToString(),
        IsActive = e.IsActive,
        JoiningDate = e.JoiningDate.ToString("yyyy-MM-dd")
    };
}

public class CreateEmployeeRequest
{
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int DepartmentId { get; set; }
    public string Role { get; set; } = "Employee";
    public string JoiningDate { get; set; } = string.Empty;
}

public class UpdateEmployeeRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int DepartmentId { get; set; }
    public string Role { get; set; } = "Employee";
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
}
