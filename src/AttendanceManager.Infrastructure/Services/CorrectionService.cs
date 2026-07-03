using AttendanceManager.Core.Entities;
using AttendanceManager.Core.Enums;
using AttendanceManager.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AttendanceManager.Infrastructure.Services;

public class CorrectionService : ICorrectionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly ILogger<CorrectionService> _logger;

    public CorrectionService(IUnitOfWork unitOfWork, INotificationService notificationService, ILogger<CorrectionService> logger)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<Correction> SubmitAsync(Correction correction)
    {
        var attendance = await _unitOfWork.Attendances.FirstOrDefaultAsync(
            a => a.EmployeeId == correction.EmployeeId && a.Date == correction.Date);

        if (attendance == null || attendance.Status != AttendanceStatus.Missing)
            throw new InvalidOperationException("Corrections can only be submitted for days marked as Missing.");

        correction.Status = CorrectionStatus.Pending;
        correction.RequestedAt = DateTime.UtcNow;
        await _unitOfWork.Corrections.AddAsync(correction);

        attendance.Status = AttendanceStatus.CorrectionPending;
        attendance.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Attendances.Update(attendance);

        await _unitOfWork.SaveChangesAsync();

        var admins = await _unitOfWork.Employees.FindAsync(e => e.Role == UserRole.Admin && e.IsActive);
        var employee = await _unitOfWork.Employees.GetByIdAsync(correction.EmployeeId);
        foreach (var admin in admins)
        {
            await _notificationService.SendNotificationAsync(admin.Id,
                "New Correction Request",
                $"{employee?.FullName} submitted a correction request for {correction.Date}.",
                NotificationType.CorrectionRequest);
        }

        _logger.LogInformation("Correction submitted by Employee {Id} for {Date}", correction.EmployeeId, correction.Date);
        return correction;
    }

    public async Task ApproveAsync(int correctionId, int adminId, string? adminComment)
    {
        var correction = await _unitOfWork.Corrections.GetByIdAsync(correctionId);
        if (correction == null) throw new InvalidOperationException("Correction request not found.");
        if (correction.Status != CorrectionStatus.Pending) throw new InvalidOperationException("Correction request is not pending.");

        correction.Status = CorrectionStatus.Approved;
        correction.AdminComment = adminComment;
        correction.DecidedById = adminId;
        correction.DecidedAt = DateTime.UtcNow;
        _unitOfWork.Corrections.Update(correction);

        var attendance = await _unitOfWork.Attendances.FirstOrDefaultAsync(
            a => a.EmployeeId == correction.EmployeeId && a.Date == correction.Date);

        if (attendance != null)
        {
            if (correction.ClaimedStatus == ClaimedStatus.OnLeave)
            {
                attendance.Status = AttendanceStatus.Leave;
            }
            else
            {
                attendance.Status = AttendanceStatus.Present;
                attendance.LoginTime = correction.ClaimedLoginTime;
                attendance.LogoutTime = correction.ClaimedLogoutTime;
                if (attendance.LoginTime.HasValue && attendance.LogoutTime.HasValue)
                {
                    attendance.TotalHours = Math.Round((attendance.LogoutTime.Value - attendance.LoginTime.Value).TotalHours, 2);
                    attendance.EffectiveHours = attendance.TotalHours;
                }
            }
            attendance.IsManualEntry = true;
            attendance.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Attendances.Update(attendance);
        }

        await _unitOfWork.SaveChangesAsync();
        await _notificationService.SendNotificationAsync(correction.EmployeeId,
            "Correction Approved", $"Your correction request for {correction.Date} has been approved.",
            NotificationType.CorrectionApproved);

        _logger.LogInformation("Correction {Id} approved by Admin {AdminId}", correctionId, adminId);
    }

    public async Task RejectAsync(int correctionId, int adminId, string? adminComment)
    {
        var correction = await _unitOfWork.Corrections.GetByIdAsync(correctionId);
        if (correction == null) throw new InvalidOperationException("Correction request not found.");
        if (correction.Status != CorrectionStatus.Pending) throw new InvalidOperationException("Correction request is not pending.");

        correction.Status = CorrectionStatus.Rejected;
        correction.AdminComment = adminComment;
        correction.DecidedById = adminId;
        correction.DecidedAt = DateTime.UtcNow;
        _unitOfWork.Corrections.Update(correction);

        var attendance = await _unitOfWork.Attendances.FirstOrDefaultAsync(
            a => a.EmployeeId == correction.EmployeeId && a.Date == correction.Date);
        if (attendance != null)
        {
            attendance.Status = AttendanceStatus.Absent;
            attendance.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Attendances.Update(attendance);
        }

        await _unitOfWork.SaveChangesAsync();
        await _notificationService.SendNotificationAsync(correction.EmployeeId,
            "Correction Rejected",
            $"Your correction request for {correction.Date} has been rejected. Reason: {adminComment}",
            NotificationType.CorrectionRejected);
    }

    public async Task<IEnumerable<Correction>> GetByEmployeeAsync(int employeeId) =>
        await _unitOfWork.Corrections.FindAsync(c => c.EmployeeId == employeeId);

    public async Task<IEnumerable<Correction>> GetPendingAsync() =>
        await _unitOfWork.Corrections.FindAsync(c => c.Status == CorrectionStatus.Pending);
}
