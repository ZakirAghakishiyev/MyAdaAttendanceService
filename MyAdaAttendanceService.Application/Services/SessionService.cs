using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Application.Services.Interfaces;
using MyAdaAttendanceService.Core.Entities;
using MyAdaAttendanceService.Core.Interfaces;

namespace MyAdaAttendanceService.Application.Services;

public class SessionService : ISessionService
{
    private readonly ILessonRepository _lessonRepository;
    private readonly ILessonSessionRepository _sessionRepository;
    private readonly ILessonEnrollmentRepository _enrollmentRepository;
    private readonly ISessionAttendanceRepository _attendanceRepository;

    public SessionService(
        ILessonRepository lessonRepository,
        ILessonSessionRepository sessionRepository,
        ILessonEnrollmentRepository enrollmentRepository,
        ISessionAttendanceRepository attendanceRepository)
    {
        _lessonRepository = lessonRepository;
        _sessionRepository = sessionRepository;
        _enrollmentRepository = enrollmentRepository;
        _attendanceRepository = attendanceRepository;
    }

    public async Task<IEnumerable<SessionDto>> GetSessionsByLessonAsync(int instructorId, int lessonId)
    {
        var lesson = await _lessonRepository.GetByIdWithDetailsAsync(lessonId);

        if (lesson == null || lesson.InstructorId != instructorId)
            throw new KeyNotFoundException("Lesson was not found.");

        var sessions = await _sessionRepository.GetByLessonIdAsync(lessonId);
        var enrollments = await _enrollmentRepository.GetByLessonIdAsync(lessonId);
        var totalStudents = enrollments.Count;

        var result = new List<SessionDto>();

        foreach (var session in sessions)
        {
            var attendances = await _attendanceRepository.GetBySessionIdAsync(session.Id);

            result.Add(new SessionDto
            {
                Id = session.Id,
                LessonId = lesson.Id,
                LessonName = lesson.Name,
                StartTime = session.Date.ToDateTime(session.StartTime),
                EndTime = session.Date.ToDateTime(session.EndTime),
                Topic = null,
                IsAttendanceActive = false,
                AttendanceActivatedAt = null,
                AttendanceDeactivatedAt = null,
                TotalStudents = totalStudents,
                PresentCount = attendances.Count(x => x.Status == AttendanceStatus.Present),
                LateCount = attendances.Count(x => x.Status == AttendanceStatus.Late),
                AbsentCount = attendances.Count(x => x.Status == AttendanceStatus.Absent),
                ExcusedCount = attendances.Count(x => x.Status == AttendanceStatus.Excused)
            });
        }

        return result;
    }

    public async Task<SessionDto> GetSessionByIdAsync(int instructorId, int sessionId)
    {
        var session = await _sessionRepository.GetByIdWithLessonAsync(sessionId);

        if (session == null || session.Lesson == null || session.Lesson.InstructorId != instructorId)
            throw new KeyNotFoundException("Session was not found.");

        var enrollments = await _enrollmentRepository.GetByLessonIdAsync(session.LessonId);
        var attendances = await _attendanceRepository.GetBySessionIdAsync(session.Id);

        return new SessionDto
        {
            Id = session.Id,
            LessonId = session.LessonId,
            LessonName = session.Lesson.Name,
            StartTime = session.Date.ToDateTime(session.StartTime),
            EndTime = session.Date.ToDateTime(session.EndTime),
            Topic = null,
            IsAttendanceActive = false,
            AttendanceActivatedAt = null,
            AttendanceDeactivatedAt = null,
            TotalStudents = enrollments.Count,
            PresentCount = attendances.Count(x => x.Status == AttendanceStatus.Present),
            LateCount = attendances.Count(x => x.Status == AttendanceStatus.Late),
            AbsentCount = attendances.Count(x => x.Status == AttendanceStatus.Absent),
            ExcusedCount = attendances.Count(x => x.Status == AttendanceStatus.Excused)
        };
    }

    public async Task<SessionDto> CreateSessionAsync(int instructorId, CreateSessionDto dto)
    {
        var lesson = await _lessonRepository.GetByIdAsync(dto.LessonId);

        if (lesson.InstructorId != instructorId)
            throw new UnauthorizedAccessException("You do not have access to this lesson.");

        var session = new LessonSession
        {
            LessonId = dto.LessonId,
            Date = DateOnly.FromDateTime(dto.StartTime),
            StartTime = TimeOnly.FromDateTime(dto.StartTime),
            EndTime = TimeOnly.FromDateTime(dto.EndTime)
        };

        await _sessionRepository.AddAsync(session);

        var enrollments = await _enrollmentRepository.GetByLessonIdAsync(dto.LessonId);

        return new SessionDto
        {
            Id = session.Id,
            LessonId = lesson.Id,
            LessonName = lesson.Name,
            StartTime = session.Date.ToDateTime(session.StartTime),
            EndTime = session.Date.ToDateTime(session.EndTime),
            Topic = null,
            IsAttendanceActive = false,
            AttendanceActivatedAt = null,
            AttendanceDeactivatedAt = null,
            TotalStudents = enrollments.Count,
            PresentCount = 0,
            LateCount = 0,
            AbsentCount = 0,
            ExcusedCount = 0
        };
    }

    public async Task ActivateAttendanceAsync(int instructorId, int sessionId)
    {
        var session = await _sessionRepository.GetByIdWithLessonAsync(sessionId);

        if (session == null || session.Lesson == null || session.Lesson.InstructorId != instructorId)
            throw new KeyNotFoundException("Session was not found.");

        // Add attendance activation fields to entity later
        await Task.CompletedTask;
    }

    public async Task DeactivateAttendanceAsync(int instructorId, int sessionId)
    {
        var session = await _sessionRepository.GetByIdWithLessonAsync(sessionId);

        if (session == null || session.Lesson == null || session.Lesson.InstructorId != instructorId)
            throw new KeyNotFoundException("Session was not found.");

        // Add attendance activation fields to entity later
        await Task.CompletedTask;
    }
}