using AttendanceManager.Core.Interfaces;
using AttendanceManager.Shared.DTOs;
using AttendanceManager.Shared.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceManager.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly IConfiguration _configuration;

    public AuthController(IEmployeeService employeeService, IConfiguration configuration)
    {
        _employeeService = employeeService;
        _configuration = configuration;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var employee = await _employeeService.AuthenticateAsync(request.Email, request.Password);
        if (employee == null)
        {
            return Ok(new LoginResponse
            {
                Success = false,
                ErrorMessage = "Invalid email or password."
            });
        }

        var jwtKey = _configuration["Jwt:Key"]!;
        var token = JwtHelper.GenerateToken(employee.Id, employee.Email, employee.Role.ToString(), jwtKey);

        return Ok(new LoginResponse
        {
            Success = true,
            Token = token,
            EmployeeId = employee.Id,
            FullName = employee.FullName,
            Role = employee.Role.ToString()
        });
    }

    [HttpPost("refresh")]
    [Authorize]
    public async Task<ActionResult<LoginResponse>> RefreshToken()
    {
        var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;
        if (string.IsNullOrEmpty(employeeIdClaim) || !int.TryParse(employeeIdClaim, out var employeeId))
            return Unauthorized(new LoginResponse { Success = false, ErrorMessage = "Invalid token." });

        var employee = await _employeeService.GetByIdAsync(employeeId);
        if (employee == null || !employee.IsActive)
            return Unauthorized(new LoginResponse { Success = false, ErrorMessage = "Employee not found or inactive." });

        var jwtKey = _configuration["Jwt:Key"]!;
        var token = JwtHelper.GenerateToken(employee.Id, employee.Email, employee.Role.ToString(), jwtKey);

        return Ok(new LoginResponse
        {
            Success = true,
            Token = token,
            EmployeeId = employee.Id,
            FullName = employee.FullName,
            Role = employee.Role.ToString()
        });
    }
}
