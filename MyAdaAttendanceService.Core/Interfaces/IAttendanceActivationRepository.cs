using MyAdaAttendanceService.Core.Entities;

namespace MyAdaAttendanceService.Core.Interfaces;

public interface IAttendanceActivationRepository : IRepository<AttendanceActivation>
{
    Task<AttendanceActivation?> GetActiveBySessionIdAsync(int sessionId);
}
