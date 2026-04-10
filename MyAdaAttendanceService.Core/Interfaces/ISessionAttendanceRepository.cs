using MyAdaAttendanceService.Core.Entities;

namespace MyAdaAttendanceService.Core.Interfaces;

public interface ISessionAttendanceRepository : IRepository<SessionAttendance>
{
    Task<SessionAttendance?> GetBySessionAndStudentAsync(int sessionId, int studentId);

    Task<List<SessionAttendance>> GetBySessionIdAsync(int sessionId);

    Task<List<SessionAttendance>> GetStudentAttendanceAsync(int studentId, int lessonId);

    Task<int> CountBySessionIdAsync(int sessionId);

    Task<int> CountBySessionIdAndStatusAsync(int sessionId, AttendanceStatus status);

    //Task<bool> ExistsAsync(int sessionId, int studentId);

    Task<List<SessionAttendance>> GetManuallyAdjustedBySessionIdAsync(int sessionId);
}