using AttendanceManager.Core.Entities;
using AttendanceManager.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AttendanceManager.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AuditService> _logger;

    public AuditService(IUnitOfWork unitOfWork, ILogger<AuditService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task LogChangeAsync(int employeeId, DateOnly date, string tableName, long recordId,
        string fieldName, string? previousValue, string? newValue, int adminId, string adminName, string? reason)
    {
        var auditLog = new AuditLog
        {
            EmployeeId = employeeId,
            Date = date,
            TableName = tableName,
            RecordId = recordId,
            FieldName = fieldName,
            PreviousValue = previousValue,
            NewValue = newValue,
            AdminId = adminId,
            AdminName = adminName,
            Reason = reason,
            Timestamp = DateTime.UtcNow
        };

        await _unitOfWork.AuditLogs.AddAsync(auditLog);
        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Audit log created: {Table}.{Field} changed for Employee {EmpId} by Admin {AdminId}",
            tableName, fieldName, employeeId, adminId);
    }

    public async Task<IEnumerable<AuditLog>> GetAuditLogsAsync(int? employeeId = null, DateOnly? startDate = null, DateOnly? endDate = null)
    {
        var logs = await _unitOfWork.AuditLogs.GetAllAsync();
        var filtered = logs.AsEnumerable();

        if (employeeId.HasValue)
            filtered = filtered.Where(l => l.EmployeeId == employeeId.Value);
        if (startDate.HasValue)
            filtered = filtered.Where(l => l.Date >= startDate.Value);
        if (endDate.HasValue)
            filtered = filtered.Where(l => l.Date <= endDate.Value);

        return filtered.OrderByDescending(l => l.Timestamp).ToList();
    }
}
