using MyAdaAttendanceService.Application.DTOs;

namespace MyAdaAttendanceService.Application.Services.Interfaces;
public interface ISessionService
{
    Task<IEnumerable<SessionDto>> GetSessionsByLessonAsync(Guid instructorId, int lessonId);

    Task<IEnumerable<SessionDto>> GetSessionsByLessonAdminAsync(int lessonId);

    Task<SessionDto> GetSessionByIdAsync(Guid instructorId, int sessionId);

    Task<SessionDto> CreateSessionAsync(Guid instructorId, int lessonId, CreateSessionDto dto);

    Task<SessionDto> UpdateSessionAsync(Guid instructorId, int sessionId, UpdateSessionDto dto);

    Task DeleteSessionAsync(Guid instructorId, int sessionId);

    Task<AttendanceActivationResultDto> ActivateAttendanceAsync(Guid instructorId, int sessionId);

    Task<AttendanceActivationResultDto> DeactivateAttendanceAsync(Guid instructorId, int sessionId);

    /// <summary>Creates sessions for each matching weekday between <paramref name="dto"/>.FromDate and ToDate; skips duplicates.</summary>
    Task<BulkGenerateSessionsResponseDto> BulkGenerateSessionsAsync(Guid instructorId, int lessonId, BulkGenerateSessionsDto dto);

    /// <summary>Registrar/office: same as <see cref="BulkGenerateSessionsAsync"/> without instructor ownership.</summary>
    Task<BulkGenerateSessionsResponseDto> BulkGenerateSessionsAdminAsync(int lessonId, BulkGenerateSessionsDto dto);
}
