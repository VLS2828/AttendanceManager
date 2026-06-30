using AttendanceManager.Core.Entities;
using AttendanceManager.Core.Enums;
using AttendanceManager.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AttendanceManager.Infrastructure.Services;

public class LeaveService : ILeaveService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly ILogger<LeaveService> _logger;

    public LeaveService(IUnitOfWork unitOfWork, INotificationService notificationService, ILogger<LeaveService> logger)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<LeaveRequest> SubmitLeaveRequestAsync(LeaveRequest request)
    {
        request.Status = LeaveStatus.Pending;
        request.CreatedAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;

        int days = 0;
        for (var d = request.StartDate; d <= request.EndDate; d = d.AddDays(1))
            days++;
        request.TotalDays = days;

        await _unitOfWork.LeaveRequests.AddAsync(request);
        await _unitOfWork.SaveChangesAsync();

        // notify admins
        var admins = await _unitOfWork.Employees.FindAsync(e => e.Role == UserRole.Admin && e.IsActive);
        var employee = await _unitOfWork.Employees.GetByIdAsync(request.EmployeeId);
        foreach (var admin in admins)
        {
            await _notificationService.SendNotificationAsync(admin.Id,
                "New Leave Request",
                $"{employee?.FullName} has requested leave from {request.StartDate} to {request.EndDate}.",
                NotificationType.LeaveRequest);
        }

        _logger.LogInformation("Leave request submitted by Employee {Id}: {Start} to {End}",
            request.EmployeeId, request.StartDate, request.EndDate);
        return request;
    }

    public async Task ApproveLeaveAsync(int leaveRequestId, int adminId, string? remarks)
    {
        var request = await _unitOfWork.LeaveRequests.GetByIdAsync(leaveRequestId);
        if (request == null) throw new InvalidOperationException("Leave request not found.");
        if (request.Status != LeaveStatus.Pending) throw new InvalidOperationException("Leave request is not pending.");

        request.Status = LeaveStatus.Approved;
        request.ApprovedById = adminId;
        request.ApprovedAt = DateTime.UtcNow;
        request.AdminRemarks = remarks;
        request.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.LeaveRequests.Update(request);

        // update leave balance (handle cross-year scenarios)
        if (request.StartDate.Year == request.EndDate.Year)
        {
            var balance = await _unitOfWork.LeaveBalances.FirstOrDefaultAsync(
                lb => lb.EmployeeId == request.EmployeeId &&
                      lb.LeaveTypeId == request.LeaveTypeId &&
                      lb.Year == request.StartDate.Year);
            if (balance != null)
            {
                balance.UsedDays += request.TotalDays;
                balance.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.LeaveBalances.Update(balance);
            }
        }
        else
        {
            var endOfStartYear = new DateOnly(request.StartDate.Year, 12, 31);
            int daysInStartYear = 0;
            for (var d = request.StartDate; d <= endOfStartYear && d <= request.EndDate; d = d.AddDays(1))
                daysInStartYear++;
            int daysInEndYear = request.TotalDays - daysInStartYear;

            var startYearBalance = await _unitOfWork.LeaveBalances.FirstOrDefaultAsync(
                lb => lb.EmployeeId == request.EmployeeId &&
                      lb.LeaveTypeId == request.LeaveTypeId &&
                      lb.Year == request.StartDate.Year);
            if (startYearBalance != null)
            {
                startYearBalance.UsedDays += daysInStartYear;
                startYearBalance.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.LeaveBalances.Update(startYearBalance);
            }

            var endYearBalance = await _unitOfWork.LeaveBalances.FirstOrDefaultAsync(
                lb => lb.EmployeeId == request.EmployeeId &&
                      lb.LeaveTypeId == request.LeaveTypeId &&
                      lb.Year == request.EndDate.Year);
            if (endYearBalance != null)
            {
                endYearBalance.UsedDays += daysInEndYear;
                endYearBalance.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.LeaveBalances.Update(endYearBalance);
            }
        }

        // update attendance for leave dates
        for (var d = request.StartDate; d <= request.EndDate; d = d.AddDays(1))
        {
            var attendance = await _unitOfWork.Attendances.FirstOrDefaultAsync(
                a => a.EmployeeId == request.EmployeeId && a.Date == d);

            if (attendance == null)
            {
                attendance = new Attendance
                {
                    EmployeeId = request.EmployeeId,
                    Date = d,
                    Status = AttendanceStatus.Leave,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Attendances.AddAsync(attendance);
            }
            else if (attendance.Status == AttendanceStatus.Absent)
            {
                attendance.Status = AttendanceStatus.Leave;
                attendance.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Attendances.Update(attendance);
            }
        }

        await _unitOfWork.SaveChangesAsync();
        await _notificationService.SendNotificationAsync(request.EmployeeId,
            "Leave Approved", $"Your leave from {request.StartDate} to {request.EndDate} has been approved.",
            NotificationType.LeaveApproved);

        _logger.LogInformation("Leave request {Id} approved by Admin {AdminId}", leaveRequestId, adminId);
    }

    public async Task RejectLeaveAsync(int leaveRequestId, int adminId, string? remarks)
    {
        var request = await _unitOfWork.LeaveRequests.GetByIdAsync(leaveRequestId);
        if (request == null) throw new InvalidOperationException("Leave request not found.");
        if (request.Status != LeaveStatus.Pending) throw new InvalidOperationException("Leave request is not pending.");

        request.Status = LeaveStatus.Rejected;
        request.ApprovedById = adminId;
        request.ApprovedAt = DateTime.UtcNow;
        request.AdminRemarks = remarks;
        request.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.LeaveRequests.Update(request);
        await _unitOfWork.SaveChangesAsync();

        await _notificationService.SendNotificationAsync(request.EmployeeId,
            "Leave Rejected", $"Your leave from {request.StartDate} to {request.EndDate} has been rejected. Reason: {remarks}",
            NotificationType.LeaveRejected);
    }

    public async Task CancelLeaveAsync(int leaveRequestId, int adminId, string? remarks)
    {
        var request = await _unitOfWork.LeaveRequests.GetByIdAsync(leaveRequestId);
        if (request == null) throw new InvalidOperationException("Leave request not found.");

        if (request.Status == LeaveStatus.Approved)
        {
            // restore leave balance
            var balance = await _unitOfWork.LeaveBalances.FirstOrDefaultAsync(
                lb => lb.EmployeeId == request.EmployeeId &&
                      lb.LeaveTypeId == request.LeaveTypeId &&
                      lb.Year == request.StartDate.Year);

            if (balance != null)
            {
                balance.UsedDays = Math.Max(0, balance.UsedDays - request.TotalDays);
                balance.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.LeaveBalances.Update(balance);
            }

            // revert attendance
            for (var d = request.StartDate; d <= request.EndDate; d = d.AddDays(1))
            {
                var attendance = await _unitOfWork.Attendances.FirstOrDefaultAsync(
                    a => a.EmployeeId == request.EmployeeId && a.Date == d && a.Status == AttendanceStatus.Leave);

                if (attendance != null)
                {
                    _unitOfWork.Attendances.Remove(attendance);
                }
            }
        }

        request.Status = LeaveStatus.Cancelled;
        request.AdminRemarks = remarks;
        request.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.LeaveRequests.Update(request);
        await _unitOfWork.SaveChangesAsync();

        await _notificationService.SendNotificationAsync(request.EmployeeId,
            "Leave Cancelled", $"Your leave from {request.StartDate} to {request.EndDate} has been cancelled.",
            NotificationType.LeaveCancelled);
    }

    public async Task<IEnumerable<LeaveRequest>> GetLeaveRequestsByEmployeeAsync(int employeeId) =>
        await _unitOfWork.LeaveRequests.FindAsync(lr => lr.EmployeeId == employeeId);

    public async Task<IEnumerable<LeaveRequest>> GetPendingLeaveRequestsAsync() =>
        await _unitOfWork.LeaveRequests.FindAsync(lr => lr.Status == LeaveStatus.Pending);

    public async Task<IEnumerable<LeaveRequest>> GetLeaveRequestsByDateRangeAsync(DateOnly startDate, DateOnly endDate) =>
        await _unitOfWork.LeaveRequests.FindAsync(lr => lr.StartDate <= endDate && lr.EndDate >= startDate);

    public async Task<IEnumerable<LeaveBalance>> GetLeaveBalancesAsync(int employeeId, int year) =>
        await _unitOfWork.LeaveBalances.FindWithIncludeAsync(
            lb => lb.EmployeeId == employeeId && lb.Year == year,
            lb => lb.LeaveType!);

    public async Task InitializeLeaveBalancesAsync(int employeeId, int year)
    {
        var leaveTypes = await _unitOfWork.LeaveTypes.FindAsync(lt => lt.IsActive);
        foreach (var lt in leaveTypes)
        {
            var existing = await _unitOfWork.LeaveBalances.FirstOrDefaultAsync(
                lb => lb.EmployeeId == employeeId && lb.LeaveTypeId == lt.Id && lb.Year == year);

            if (existing == null)
            {
                await _unitOfWork.LeaveBalances.AddAsync(new LeaveBalance
                {
                    EmployeeId = employeeId,
                    LeaveTypeId = lt.Id,
                    Year = year,
                    TotalDays = lt.DefaultDaysPerYear,
                    UsedDays = 0
                });
            }
        }
        await _unitOfWork.SaveChangesAsync();
    }
}
