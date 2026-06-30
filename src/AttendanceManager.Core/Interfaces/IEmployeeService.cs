using AttendanceManager.Core.Entities;

namespace AttendanceManager.Core.Interfaces;

public interface IEmployeeService
{
    Task<Employee?> AuthenticateAsync(string email, string password);
    Task<Employee?> GetByIdAsync(int id);
    Task<IEnumerable<Employee>> GetAllActiveAsync();
    Task<Employee> CreateAsync(Employee employee, string password);
    Task UpdateAsync(Employee employee);
    Task DeactivateAsync(int employeeId);
    Task ChangePasswordAsync(int employeeId, string currentPassword, string newPassword);
    Task ResetPasswordAsync(int employeeId, string newPassword, int adminId);
}
