using AttendanceManager.Core.Entities;

namespace AttendanceManager.Core.Interfaces;

public interface ICorrectionService
{
    Task<Correction> SubmitAsync(Correction correction);
    Task ApproveAsync(int correctionId, int adminId, string? adminComment);
    Task RejectAsync(int correctionId, int adminId, string? adminComment);
    Task<IEnumerable<Correction>> GetByEmployeeAsync(int employeeId);
    Task<IEnumerable<Correction>> GetPendingAsync();
}
