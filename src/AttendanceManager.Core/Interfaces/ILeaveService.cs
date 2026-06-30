using AttendanceManager.Core.Entities;
using AttendanceManager.Core.Enums;

namespace AttendanceManager.Core.Interfaces;

public interface ILeaveService
{
    Task<LeaveRequest> SubmitLeaveRequestAsync(LeaveRequest request);
    Task ApproveLeaveAsync(int leaveRequestId, int adminId, string? remarks);
    Task RejectLeaveAsync(int leaveRequestId, int adminId, string? remarks);
    Task CancelLeaveAsync(int leaveRequestId, int adminId, string? remarks);
    Task<IEnumerable<LeaveRequest>> GetLeaveRequestsByEmployeeAsync(int employeeId);
    Task<IEnumerable<LeaveRequest>> GetPendingLeaveRequestsAsync();
    Task<IEnumerable<LeaveRequest>> GetLeaveRequestsByDateRangeAsync(DateOnly startDate, DateOnly endDate);
    Task<IEnumerable<LeaveBalance>> GetLeaveBalancesAsync(int employeeId, int year);
    Task InitializeLeaveBalancesAsync(int employeeId, int year);
}
