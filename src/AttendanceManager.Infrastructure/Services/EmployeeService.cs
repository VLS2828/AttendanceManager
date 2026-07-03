using AttendanceManager.Core.Entities;
using AttendanceManager.Core.Interfaces;
using AttendanceManager.Shared.Helpers;
using Microsoft.Extensions.Logging;

namespace AttendanceManager.Infrastructure.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EmployeeService> _logger;

    public EmployeeService(IUnitOfWork unitOfWork, ILogger<EmployeeService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Employee?> AuthenticateAsync(string email, string password)
    {
        var employee = await _unitOfWork.Employees.FirstOrDefaultAsync(
            e => e.Email.ToLower() == email.ToLower() && e.IsActive);

        if (employee == null)
        {
            _logger.LogWarning("Authentication failed: Employee not found for email {Email}", email);
            return null;
        }

        if (!PasswordHelper.VerifyPassword(password, employee.PasswordHash))
        {
            _logger.LogWarning("Authentication failed: Invalid password for {Email}", email);
            return null;
        }

        _logger.LogInformation("Employee {Email} authenticated successfully", email);
        return employee;
    }

    public async Task<Employee?> AuthenticateByPinAsync(string email, string pin)
    {
        var employee = await _unitOfWork.Employees.FirstOrDefaultAsync(
            e => e.Email.ToLower() == email.ToLower() && e.IsActive);

        if (employee == null || string.IsNullOrEmpty(employee.PinHash))
        {
            _logger.LogWarning("PIN authentication failed: no employee/PIN for email {Email}", email);
            return null;
        }

        if (!PasswordHelper.VerifyPassword(pin, employee.PinHash))
        {
            _logger.LogWarning("PIN authentication failed: invalid PIN for {Email}", email);
            return null;
        }

        _logger.LogInformation("Employee {Email} authenticated via PIN", email);
        return employee;
    }

    public async Task<string> SetPinAsync(int employeeId, string? pin)
    {
        var employee = await _unitOfWork.Employees.GetByIdAsync(employeeId);
        if (employee == null) throw new InvalidOperationException("Employee not found.");

        var effectivePin = string.IsNullOrWhiteSpace(pin)
            ? System.Security.Cryptography.RandomNumberGenerator.GetInt32(1000, 10000).ToString()
            : pin;

        employee.PinHash = PasswordHelper.HashPassword(effectivePin);
        employee.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Employees.Update(employee);
        await _unitOfWork.SaveChangesAsync();

        return effectivePin;
    }

    public async Task<Employee?> GetByIdAsync(int id) =>
        await _unitOfWork.Employees.GetByIdAsync(id);

    public async Task<IEnumerable<Employee>> GetAllActiveAsync() =>
        await _unitOfWork.Employees.FindAsync(e => e.IsActive);

    public async Task<Employee> CreateAsync(Employee employee, string password)
    {
        var existing = await _unitOfWork.Employees.FirstOrDefaultAsync(e => e.Email.ToLower() == employee.Email.ToLower());
        if (existing != null)
            throw new InvalidOperationException($"Employee with email {employee.Email} already exists.");

        employee.PasswordHash = PasswordHelper.HashPassword(password);
        employee.CreatedAt = DateTime.UtcNow;
        employee.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Employees.AddAsync(employee);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Employee created: {Code} - {Name}", employee.EmployeeCode, employee.FullName);
        return employee;
    }

    public async Task UpdateAsync(Employee employee)
    {
        employee.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Employees.Update(employee);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeactivateAsync(int employeeId)
    {
        var employee = await _unitOfWork.Employees.GetByIdAsync(employeeId);
        if (employee == null) throw new InvalidOperationException("Employee not found.");

        employee.IsActive = false;
        employee.TerminationDate = DateTime.UtcNow;
        employee.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Employees.Update(employee);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task ChangePasswordAsync(int employeeId, string currentPassword, string newPassword)
    {
        var employee = await _unitOfWork.Employees.GetByIdAsync(employeeId);
        if (employee == null) throw new InvalidOperationException("Employee not found.");

        if (!PasswordHelper.VerifyPassword(currentPassword, employee.PasswordHash))
            throw new InvalidOperationException("Current password is incorrect.");

        employee.PasswordHash = PasswordHelper.HashPassword(newPassword);
        employee.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Employees.Update(employee);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task ResetPasswordAsync(int employeeId, string newPassword, int adminId)
    {
        var employee = await _unitOfWork.Employees.GetByIdAsync(employeeId);
        if (employee == null) throw new InvalidOperationException("Employee not found.");

        employee.PasswordHash = PasswordHelper.HashPassword(newPassword);
        employee.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Employees.Update(employee);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Password reset for Employee {Id} by Admin {AdminId}", employeeId, adminId);
    }
}
