using AttendanceManager.Core.Entities;

namespace AttendanceManager.Core.Interfaces;

public interface IAuditService
{
    Task LogChangeAsync(int employeeId, DateOnly date, string tableName, long recordId,
        string fieldName, string? previousValue, string? newValue, int adminId, string adminName, string? reason);
    Task<IEnumerable<AuditLog>> GetAuditLogsAsync(int? employeeId = null, DateOnly? startDate = null, DateOnly? endDate = null);
}
