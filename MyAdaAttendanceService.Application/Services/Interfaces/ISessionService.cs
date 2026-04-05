using MyAdaAttendanceService.Application.DTOs;

namespace MyAdaAttendanceService.Application.Services.Interfaces;

public interface ISessionService
{
    Task<IEnumerable<SessionDto>> GetSessionsByLessonAsync(int instructorId, int lessonId);

    Task<SessionDto> GetSessionByIdAsync(int instructorId, int sessionId);

    Task<SessionDto> CreateSessionAsync(int instructorId, CreateSessionDto dto);

    Task ActivateAttendanceAsync(int instructorId, int sessionId);

    Task DeactivateAttendanceAsync(int instructorId, int sessionId);
}
