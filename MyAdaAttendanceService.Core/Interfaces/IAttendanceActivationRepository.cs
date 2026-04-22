using MyAdaAttendanceService.Core.Entities;

namespace MyAdaAttendanceService.Core.Interfaces;

public interface IAttendanceActivationRepository : IRepository<AttendanceActivation>
{
    Task<AttendanceActivation?> GetActiveBySessionIdAsync(int sessionId);

    /// <summary>Completed activation for a session and round (ended and not active).</summary>
    Task<bool> HasClosedActivationForRoundAsync(int sessionId, byte round);
}
