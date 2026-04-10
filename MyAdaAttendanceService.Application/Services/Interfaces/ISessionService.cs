using MyAdaAttendanceService.Application.DTOs;

namespace MyAdaAttendanceService.Application.Services.Interfaces;
public interface ISessionService
{
    Task<IEnumerable<SessionDto>> GetSessionsByLessonAsync(int instructorId, int lessonId);

    Task<IEnumerable<SessionDto>> GetSessionsByLessonAdminAsync(int lessonId);

    Task<SessionDto> GetSessionByIdAsync(int instructorId, int sessionId);

    Task<SessionDto> CreateSessionAsync(int instructorId, CreateSessionDto dto);

    Task<SessionDto> UpdateSessionAsync(int instructorId, int sessionId, UpdateSessionDto dto);

    Task DeleteSessionAsync(int instructorId, int sessionId);

    Task<AttendanceActivationResultDto> ActivateAttendanceAsync(int instructorId, int sessionId);

    Task<AttendanceActivationResultDto> DeactivateAttendanceAsync(int instructorId, int sessionId);
}
