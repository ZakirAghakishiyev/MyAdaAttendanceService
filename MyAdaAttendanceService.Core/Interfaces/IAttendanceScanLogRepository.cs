using MyAdaAttendanceService.Core.Entities;

namespace MyAdaAttendanceService.Core.Interfaces;

public interface IAttendanceScanLogRepository : IRepository<AttendanceScanLog>
{
    Task<bool> ExistsAcceptedByTokenAsync(int sessionId, Guid studentId, string tokenJti);
    Task<int> CountAcceptedScansAsync(int sessionId, Guid studentId, int activationId);
    Task<List<AttendanceScanLog>> GetAcceptedBySessionAndActivationAsync(int sessionId, int activationId);

    Task<int> CountDistinctRoundsScannedAsync(int sessionId, Guid studentId);

    Task<bool> HasAcceptedScanInRoundAsync(int sessionId, Guid studentId, byte round);

    Task<IReadOnlyList<AttendanceScanLog>> GetAcceptedBySessionIdAsync(int sessionId);
}
