using AttendanceManager.Core.Entities;

namespace AttendanceManager.Core.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepository<Employee> Employees { get; }
    IRepository<Department> Departments { get; }
    IRepository<Attendance> Attendances { get; }
    IRepository<LeaveType> LeaveTypes { get; }
    IRepository<LeaveRequest> LeaveRequests { get; }
    IRepository<LeaveBalance> LeaveBalances { get; }
    IRepository<Holiday> Holidays { get; }
    IRepository<AuditLog> AuditLogs { get; }
    IRepository<Notification> Notifications { get; }
    IRepository<AppSetting> AppSettings { get; }
    IRepository<IdleLog> IdleLogs { get; }
    Task<int> SaveChangesAsync();
}
