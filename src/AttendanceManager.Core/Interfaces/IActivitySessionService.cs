using AttendanceManager.Core.Entities;

namespace AttendanceManager.Core.Interfaces;

public interface IActivitySessionService
{
    Task SyncAsync(int employeeId, List<ActivitySession> intervals);
}
