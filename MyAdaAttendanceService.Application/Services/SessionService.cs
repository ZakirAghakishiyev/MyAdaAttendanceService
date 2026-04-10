using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Application.Services.Interfaces;
using MyAdaAttendanceService.Core.Entities;
using MyAdaAttendanceService.Core.Interfaces;

namespace MyAdaAttendanceService.Application.Services;

public class SessionService : ISessionService
{
    private readonly ILessonSessionRepository _sessionRepository;
    private readonly ILessonRepository _lessonRepository;
    private readonly ISessionAttendanceRepository _attendanceRepository;
    private readonly ILessonEnrollmentRepository _enrollmentRepository;

    public SessionService(
        ILessonSessionRepository sessionRepository,
        ILessonRepository lessonRepository,
        ISessionAttendanceRepository attendanceRepository,
        ILessonEnrollmentRepository enrollmentRepository)
    {
        _sessionRepository = sessionRepository;
        _lessonRepository = lessonRepository;
        _attendanceRepository = attendanceRepository;
        _enrollmentRepository = enrollmentRepository;
    }

    public async Task<IEnumerable<SessionDto>> GetSessionsByLessonAsync(int instructorId, int lessonId)
    {
        var lesson = await _lessonRepository.GetByIdAsync(lessonId);

        if (lesson.InstructorId != instructorId)
            throw new UnauthorizedAccessException("You do not own this lesson.");

        var sessions = await _sessionRepository.GetByLessonIdAsync(lessonId);
        var enrollmentCount = (await _enrollmentRepository.GetByLessonIdAsync(lessonId)).Count;

        var sessionIds = sessions.Select(s => s.Id).ToList();
        var allAttendances = await _attendanceRepository.GetAllAsync(
            predicate: a => sessionIds.Contains(a.SessionId));

        var grouped = allAttendances.GroupBy(a => a.SessionId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return sessions.Select(s =>
        {
            grouped.TryGetValue(s.Id, out var records);
            return MapToSessionDto(s, lesson.Name, enrollmentCount, records);
        });
    }

    public async Task<IEnumerable<SessionDto>> GetSessionsByLessonAdminAsync(int lessonId)
    {
        var lesson = await _lessonRepository.GetByIdAsync(lessonId);
        var sessions = await _sessionRepository.GetByLessonIdAsync(lessonId);
        var enrollmentCount = (await _enrollmentRepository.GetByLessonIdAsync(lessonId)).Count;

        var sessionIds = sessions.Select(s => s.Id).ToList();
        var allAttendances = await _attendanceRepository.GetAllAsync(
            predicate: a => sessionIds.Contains(a.SessionId));

        var grouped = allAttendances.GroupBy(a => a.SessionId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return sessions.Select(s =>
        {
            grouped.TryGetValue(s.Id, out var records);
            return MapToSessionDto(s, lesson.Name, enrollmentCount, records);
        });
    }

    public async Task<SessionDto> GetSessionByIdAsync(int instructorId, int sessionId)
    {
        var session = await GetVerifiedSessionAsync(instructorId, sessionId);
        var attendances = await _attendanceRepository.GetBySessionIdAsync(sessionId);
        var enrollmentCount = (await _enrollmentRepository.GetByLessonIdAsync(session.LessonId)).Count;

        return MapToSessionDto(session, session.Lesson!.Name, enrollmentCount, attendances);
    }

    public async Task<SessionDto> CreateSessionAsync(int instructorId, CreateSessionDto dto)
    {
        var lesson = await _lessonRepository.GetByIdAsync(dto.LessonId);

        if (lesson.InstructorId != instructorId)
            throw new UnauthorizedAccessException("You do not own this lesson.");

        var session = new LessonSession
        {
            LessonId = dto.LessonId,
            Date = DateOnly.FromDateTime(dto.StartTime),
            StartTime = TimeOnly.FromDateTime(dto.StartTime),
            EndTime = TimeOnly.FromDateTime(dto.EndTime),
            Topic = dto.Topic
        };

        await _sessionRepository.AddAsync(session);

        return MapToSessionDto(session, lesson.Name, 0, null);
    }

    public async Task<SessionDto> UpdateSessionAsync(int instructorId, int sessionId, UpdateSessionDto dto)
    {
        var session = await GetVerifiedSessionAsync(instructorId, sessionId);

        session.Date = DateOnly.FromDateTime(dto.StartTime);
        session.StartTime = TimeOnly.FromDateTime(dto.StartTime);
        session.EndTime = TimeOnly.FromDateTime(dto.EndTime);
        session.Topic = dto.Topic;

        await _sessionRepository.UpdateAsync(session);

        var attendances = await _attendanceRepository.GetBySessionIdAsync(sessionId);
        var enrollmentCount = (await _enrollmentRepository.GetByLessonIdAsync(session.LessonId)).Count;

        return MapToSessionDto(session, session.Lesson!.Name, enrollmentCount, attendances);
    }

    public async Task DeleteSessionAsync(int instructorId, int sessionId)
    {
        var session = await GetVerifiedSessionAsync(instructorId, sessionId);
        await _sessionRepository.RemoveAsync(session);
    }

    public async Task<AttendanceActivationResultDto> ActivateAttendanceAsync(int instructorId, int sessionId)
    {
        var session = await GetVerifiedSessionAsync(instructorId, sessionId);

        if (session.IsAttendanceActive)
            throw new InvalidOperationException("Attendance is already active for this session.");

        session.IsAttendanceActive = true;
        session.AttendanceActivatedAt = DateTime.UtcNow;
        session.AttendanceDeactivatedAt = null;

        await _sessionRepository.UpdateAsync(session);

        return new AttendanceActivationResultDto
        {
            SessionId = session.Id,
            IsAttendanceActive = true,
            AttendanceActivatedAt = session.AttendanceActivatedAt,
            Message = "Attendance activated successfully."
        };
    }

    public async Task<AttendanceActivationResultDto> DeactivateAttendanceAsync(int instructorId, int sessionId)
    {
        var session = await GetVerifiedSessionAsync(instructorId, sessionId);

        if (!session.IsAttendanceActive)
            throw new InvalidOperationException("Attendance is not active for this session.");

        session.IsAttendanceActive = false;
        session.AttendanceDeactivatedAt = DateTime.UtcNow;

        await _sessionRepository.UpdateAsync(session);

        return new AttendanceActivationResultDto
        {
            SessionId = session.Id,
            IsAttendanceActive = false,
            AttendanceActivatedAt = session.AttendanceActivatedAt,
            AttendanceDeactivatedAt = session.AttendanceDeactivatedAt,
            Message = "Attendance deactivated successfully."
        };
    }

    private async Task<LessonSession> GetVerifiedSessionAsync(int instructorId, int sessionId)
    {
        var session = await _sessionRepository.GetByIdWithLessonAsync(sessionId)
            ?? throw new KeyNotFoundException($"Session {sessionId} not found.");

        if (session.Lesson!.InstructorId != instructorId)
            throw new UnauthorizedAccessException("You do not own this session.");

        return session;
    }

    private static SessionDto MapToSessionDto(
        LessonSession session,
        string lessonName,
        int enrollmentCount,
        List<SessionAttendance>? attendances)
    {
        return new SessionDto
        {
            Id = session.Id,
            LessonId = session.LessonId,
            LessonName = lessonName,
            StartTime = session.Date.ToDateTime(session.StartTime),
            EndTime = session.Date.ToDateTime(session.EndTime),
            Topic = session.Topic,
            IsAttendanceActive = session.IsAttendanceActive,
            AttendanceActivatedAt = session.AttendanceActivatedAt,
            AttendanceDeactivatedAt = session.AttendanceDeactivatedAt,
            TotalStudents = enrollmentCount,
            PresentCount = attendances?.Count(r => r.Status == AttendanceStatus.Present) ?? 0,
            LateCount = attendances?.Count(r => r.Status == AttendanceStatus.Late) ?? 0,
            AbsentCount = attendances?.Count(r => r.Status == AttendanceStatus.Absent) ?? 0,
            ExcusedCount = attendances?.Count(r => r.Status == AttendanceStatus.Excused) ?? 0
        };
    }
}
