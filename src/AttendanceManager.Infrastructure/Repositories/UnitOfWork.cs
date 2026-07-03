using AttendanceManager.Core.Entities;
using AttendanceManager.Core.Interfaces;
using AttendanceManager.Infrastructure.Data;

namespace AttendanceManager.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IRepository<Employee>? _employees;
    private IRepository<Department>? _departments;
    private IRepository<Attendance>? _attendances;
    private IRepository<LeaveType>? _leaveTypes;
    private IRepository<LeaveRequest>? _leaveRequests;
    private IRepository<LeaveBalance>? _leaveBalances;
    private IRepository<Holiday>? _holidays;
    private IRepository<AuditLog>? _auditLogs;
    private IRepository<Notification>? _notifications;
    private IRepository<AppSetting>? _appSettings;
    private IRepository<IdleLog>? _idleLogs;
    private IRepository<Correction>? _corrections;
    private IRepository<WorkSummary>? _workSummaries;
    private IRepository<WorkSummaryEntry>? _workSummaryEntries;
    private IRepository<ActivitySession>? _activitySessions;

    public UnitOfWork(AppDbContext context) => _context = context;

    public IRepository<Employee> Employees => _employees ??= new Repository<Employee>(_context);
    public IRepository<Department> Departments => _departments ??= new Repository<Department>(_context);
    public IRepository<Attendance> Attendances => _attendances ??= new Repository<Attendance>(_context);
    public IRepository<LeaveType> LeaveTypes => _leaveTypes ??= new Repository<LeaveType>(_context);
    public IRepository<LeaveRequest> LeaveRequests => _leaveRequests ??= new Repository<LeaveRequest>(_context);
    public IRepository<LeaveBalance> LeaveBalances => _leaveBalances ??= new Repository<LeaveBalance>(_context);
    public IRepository<Holiday> Holidays => _holidays ??= new Repository<Holiday>(_context);
    public IRepository<AuditLog> AuditLogs => _auditLogs ??= new Repository<AuditLog>(_context);
    public IRepository<Notification> Notifications => _notifications ??= new Repository<Notification>(_context);
    public IRepository<AppSetting> AppSettings => _appSettings ??= new Repository<AppSetting>(_context);
    public IRepository<IdleLog> IdleLogs => _idleLogs ??= new Repository<IdleLog>(_context);
    public IRepository<Correction> Corrections => _corrections ??= new Repository<Correction>(_context);
    public IRepository<WorkSummary> WorkSummaries => _workSummaries ??= new Repository<WorkSummary>(_context);
    public IRepository<WorkSummaryEntry> WorkSummaryEntries => _workSummaryEntries ??= new Repository<WorkSummaryEntry>(_context);
    public IRepository<ActivitySession> ActivitySessions => _activitySessions ??= new Repository<ActivitySession>(_context);

    public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();

    public void Dispose() => _context.Dispose();
}
