namespace AttendanceManager.Shared.DTOs;

public class EmployeeDto
{
    public int Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Department { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string JoiningDate { get; set; } = string.Empty;
}
