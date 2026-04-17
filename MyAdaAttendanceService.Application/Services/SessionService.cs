using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Application.Services.Interfaces;
using MyAdaAttendanceService.Core.Entities;
using MyAdaAttendanceService.Core.Interfaces;

namespace MyAdaAttendanceService.Application.Services;

public class SessionService : ISessionService
{
    private const int MaxCalendarSpanDays = 731;

    private const int MaxSessionsPerRequest = 2000;

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

    public async Task<IEnumerable<SessionDto>> GetSessionsByLessonAsync(Guid instructorId, int lessonId)
    {
        var lesson = await _lessonRepository.GetByIdWithCourseAsync(lessonId)
            ?? throw new KeyNotFoundException($"Lesson {lessonId} not found.");

        if (lesson.InstructorId != instructorId)
            throw new UnauthorizedAccessException("You do not own this lesson.");

        var lessonTitle = lesson.Course!.Name;

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
            return MapToSessionDto(s, lessonTitle, enrollmentCount, records);
        });
    }

    public async Task<IEnumerable<SessionDto>> GetSessionsByLessonAdminAsync(int lessonId)
    {
        var lesson = await _lessonRepository.GetByIdWithCourseAsync(lessonId)
            ?? throw new KeyNotFoundException($"Lesson {lessonId} not found.");
        var lessonTitle = lesson.Course!.Name;

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
            return MapToSessionDto(s, lessonTitle, enrollmentCount, records);
        });
    }

    public async Task<SessionDto> GetSessionByIdAsync(Guid instructorId, int sessionId)
    {
        var session = await GetVerifiedSessionAsync(instructorId, sessionId);
        var attendances = await _attendanceRepository.GetBySessionIdAsync(sessionId);
        var enrollmentCount = (await _enrollmentRepository.GetByLessonIdAsync(session.LessonId)).Count;

        return MapToSessionDto(session, session.Lesson!.Course!.Name, enrollmentCount, attendances);
    }

    public async Task<SessionDto> CreateSessionAsync(Guid instructorId, int lessonId, CreateSessionDto dto)
    {
        var lesson = await _lessonRepository.GetByIdWithCourseAsync(lessonId)
            ?? throw new KeyNotFoundException($"Lesson {lessonId} not found.");

        if (lesson.InstructorId != instructorId)
            throw new UnauthorizedAccessException("You do not own this lesson.");

        var session = new LessonSession
        {
            LessonId = lessonId,
            Date = DateOnly.FromDateTime(dto.StartTime),
            StartTime = TimeOnly.FromDateTime(dto.StartTime),
            EndTime = TimeOnly.FromDateTime(dto.EndTime),
            Topic = dto.Topic
        };

        await _sessionRepository.AddAsync(session);

        return MapToSessionDto(session, lesson.Course!.Name, 0, null);
    }

    public async Task<SessionDto> UpdateSessionAsync(Guid instructorId, int sessionId, UpdateSessionDto dto)
    {
        var session = await GetVerifiedSessionAsync(instructorId, sessionId);

        session.Date = DateOnly.FromDateTime(dto.StartTime);
        session.StartTime = TimeOnly.FromDateTime(dto.StartTime);
        session.EndTime = TimeOnly.FromDateTime(dto.EndTime);
        session.Topic = dto.Topic;

        await _sessionRepository.UpdateAsync(session);

        var attendances = await _attendanceRepository.GetBySessionIdAsync(sessionId);
        var enrollmentCount = (await _enrollmentRepository.GetByLessonIdAsync(session.LessonId)).Count;

        return MapToSessionDto(session, session.Lesson!.Course!.Name, enrollmentCount, attendances);
    }

    public async Task DeleteSessionAsync(Guid instructorId, int sessionId)
    {
        var session = await GetVerifiedSessionAsync(instructorId, sessionId);
        await _sessionRepository.RemoveAsync(session);
    }

    public async Task<BulkGenerateSessionsResponseDto> BulkGenerateSessionsAsync(
        Guid instructorId,
        int lessonId,
        BulkGenerateSessionsDto dto)
    {
        var lesson = await _lessonRepository.GetByIdWithCourseAsync(lessonId)
            ?? throw new KeyNotFoundException($"Lesson {lessonId} not found.");

        if (lesson.InstructorId != instructorId)
            throw new UnauthorizedAccessException("You do not own this lesson.");

        return await BulkGenerateCoreAsync(lesson, dto);
    }

    public async Task<BulkGenerateSessionsResponseDto> BulkGenerateSessionsAdminAsync(int lessonId, BulkGenerateSessionsDto dto)
    {
        var lesson = await _lessonRepository.GetByIdWithCourseAsync(lessonId)
            ?? throw new KeyNotFoundException($"Lesson {lessonId} not found.");

        return await BulkGenerateCoreAsync(lesson, dto);
    }

    public async Task<AttendanceActivationResultDto> ActivateAttendanceAsync(Guid instructorId, int sessionId)
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

    public async Task<AttendanceActivationResultDto> DeactivateAttendanceAsync(Guid instructorId, int sessionId)
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

    private async Task<LessonSession> GetVerifiedSessionAsync(Guid instructorId, int sessionId)
    {
        var session = await _sessionRepository.GetByIdWithLessonAsync(sessionId)
            ?? throw new KeyNotFoundException($"Session {sessionId} not found.");

        if (session.Lesson!.InstructorId != instructorId)
            throw new UnauthorizedAccessException("You do not own this session.");

        return session;
    }

    private async Task<BulkGenerateSessionsResponseDto> BulkGenerateCoreAsync(Lesson lesson, BulkGenerateSessionsDto dto)
    {
        ValidateBulkGenerateDto(dto);

        var spanDays = dto.ToDate.DayNumber - dto.FromDate.DayNumber;
        if (spanDays > MaxCalendarSpanDays)
            throw new ArgumentException($"Date range cannot exceed {MaxCalendarSpanDays} days.");

        var distinctSlots = dto.WeeklySlots
            .DistinctBy(s => (s.DayOfWeek, s.StartTime, s.EndTime))
            .ToList();

        var proposed = BuildProposedSessions(lesson.Id, dto.FromDate, dto.ToDate, distinctSlots, dto.Topic);
        if (proposed.Count > MaxSessionsPerRequest)
            throw new ArgumentException($"Would create more than {MaxSessionsPerRequest} sessions; narrow the range or weekly pattern.");

        var existing = await _sessionRepository.GetByLessonIdAsync(lesson.Id);
        var existingKeys = existing.Select(s => (s.Date, s.StartTime, s.EndTime)).ToHashSet();

        var toInsert = new List<LessonSession>();
        var skipped = 0;
        foreach (var session in proposed)
        {
            var key = (session.Date, session.StartTime, session.EndTime);
            if (existingKeys.Contains(key))
            {
                skipped++;
                continue;
            }

            existingKeys.Add(key);
            toInsert.Add(session);
        }

        if (toInsert.Count > 0)
            await _sessionRepository.AddRangeAsync(toInsert);

        var lessonTitle = lesson.Course!.Name;
        var enrollmentCount = (await _enrollmentRepository.GetByLessonIdAsync(lesson.Id)).Count;

        var createdDtos = toInsert
            .OrderBy(s => s.Date)
            .ThenBy(s => s.StartTime)
            .Select(s => MapToSessionDto(s, lessonTitle, enrollmentCount, null))
            .ToList();

        return new BulkGenerateSessionsResponseDto
        {
            CreatedCount = toInsert.Count,
            SkippedDuplicateCount = skipped,
            CreatedSessions = createdDtos
        };
    }

    private static void ValidateBulkGenerateDto(BulkGenerateSessionsDto dto)
    {
        if (dto.FromDate > dto.ToDate)
            throw new ArgumentException("FromDate must be on or before ToDate.");

        if (dto.WeeklySlots == null || dto.WeeklySlots.Count == 0)
            throw new ArgumentException("At least one weekly slot is required.");

        foreach (var slot in dto.WeeklySlots)
        {
            if (slot.StartTime >= slot.EndTime)
                throw new ArgumentException($"StartTime must be before EndTime for {slot.DayOfWeek}.");
        }
    }

    private static List<LessonSession> BuildProposedSessions(
        int lessonId,
        DateOnly fromDate,
        DateOnly toDate,
        IReadOnlyList<WeeklySessionSlotDto> slots,
        string? topic)
    {
        var result = new List<LessonSession>();
        for (var d = fromDate; d <= toDate; d = d.AddDays(1))
        {
            foreach (var slot in slots)
            {
                if (d.DayOfWeek != slot.DayOfWeek)
                    continue;

                result.Add(new LessonSession
                {
                    LessonId = lessonId,
                    Date = d,
                    StartTime = slot.StartTime,
                    EndTime = slot.EndTime,
                    Topic = topic
                });
            }
        }

        return result;
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
