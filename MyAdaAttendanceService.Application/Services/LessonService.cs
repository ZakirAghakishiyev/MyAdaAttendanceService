using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Application.Services.Interfaces;
using MyAdaAttendanceService.Core.Entities;
using MyAdaAttendanceService.Core.Interfaces;

namespace MyAdaAttendanceService.Application.Services;

public class LessonService : ILessonService
{
    private readonly ILessonRepository _lessonRepository;

    public LessonService(ILessonRepository lessonRepository)
    {
        _lessonRepository = lessonRepository;
    }

    public async Task<IEnumerable<LessonDto>> GetMyLessonsAsync(int instructorId)
    {
        var lessons = await _lessonRepository.GetByInstructorIdAsync(instructorId);

        var result = new List<LessonDto>();

        foreach (var lesson in lessons)
        {
            var detailedLesson = await _lessonRepository.GetByIdWithDetailsAsync(lesson.Id);

            result.Add(new LessonDto
            {
                Id = lesson.Id,
                Name = lesson.Name,
                Code = lesson.Code,
                InstructorId = lesson.InstructorId,
                Sessions = detailedLesson?.Sessions?
                    .OrderBy(x => x.Date)
                    .ThenBy(x => x.StartTime)
                    .Select(x => new SessionShortDto
                    {
                        Id = x.Id,
                        Date = x.Date,
                        StartTime = x.StartTime,
                        EndTime = x.EndTime
                    })
                    .ToList()
            });
        }

        return result;
    }

    public async Task<LessonDto> GetMyLessonByIdAsync(int instructorId, int lessonId)
    {
        var lesson = await _lessonRepository.GetByIdWithDetailsAsync(lessonId);

        if (lesson == null || lesson.InstructorId != instructorId)
            throw new KeyNotFoundException("Lesson was not found.");

        return new LessonDto
        {
            Id = lesson.Id,
            Name = lesson.Name,
            Code = lesson.Code,
            InstructorId = lesson.InstructorId,
            Sessions = lesson.Sessions?
                .OrderBy(x => x.Date)
                .ThenBy(x => x.StartTime)
                .Select(x => new SessionShortDto
                {
                    Id = x.Id,
                    Date = x.Date,
                    StartTime = x.StartTime,
                    EndTime = x.EndTime
                })
                .ToList()
        };
    }
}



public class EnrollmentService : IEnrollmentService
{
    private readonly ILessonEnrollmentRepository _enrollmentRepository;
    private readonly IStudentDirectoryService _studentDirectoryService;

    public EnrollmentService(
        ILessonEnrollmentRepository enrollmentRepository,
        IStudentDirectoryService studentDirectoryService)
    {
        _enrollmentRepository = enrollmentRepository;
        _studentDirectoryService = studentDirectoryService;
    }

    public async Task<bool> IsStudentEnrolledAsync(int studentId, int lessonId)
    {
        return await _enrollmentRepository.ExistsAsync(lessonId, studentId);
    }

    public async Task<IEnumerable<StudentDto>> GetStudentsByLessonAsync(int lessonId)
    {
        var enrollments = await _enrollmentRepository.GetByLessonIdAsync(lessonId);
        var studentIds = enrollments.Select(x => x.StudentId).Distinct().ToList();

        var students = await _studentDirectoryService.GetStudentsByIdsAsync(studentIds);

        return studentIds
            .Where(id => students.ContainsKey(id))
            .Select(id => students[id])
            .ToList();
    }
}


public class AttendanceService : IAttendanceService
{
    private readonly ISessionAttendanceRepository _attendanceRepository;
    private readonly ILessonSessionRepository _sessionRepository;
    private readonly ILessonEnrollmentRepository _enrollmentRepository;
    private readonly IStudentDirectoryService _studentDirectoryService;

    public AttendanceService(
        ISessionAttendanceRepository attendanceRepository,
        ILessonSessionRepository sessionRepository,
        ILessonEnrollmentRepository enrollmentRepository,
        IStudentDirectoryService studentDirectoryService)
    {
        _attendanceRepository = attendanceRepository;
        _sessionRepository = sessionRepository;
        _enrollmentRepository = enrollmentRepository;
        _studentDirectoryService = studentDirectoryService;
    }

    public async Task<IEnumerable<AttendanceDto>> GetSessionAttendanceAsync(int instructorId, int sessionId)
    {
        var session = await _sessionRepository.GetByIdWithLessonAsync(sessionId);

        if (session == null || session.Lesson == null || session.Lesson.InstructorId != instructorId)
            throw new KeyNotFoundException("Session was not found.");

        var attendances = await _attendanceRepository.GetBySessionIdAsync(sessionId);
        var studentIds = attendances.Select(x => x.StudentId).Distinct().ToList();
        var students = await _studentDirectoryService.GetStudentsByIdsAsync(studentIds);

        return attendances.Select(a =>
        {
            students.TryGetValue(a.StudentId, out var student);

            return new AttendanceDto
            {
                Id = a.Id,
                SessionId = a.SessionId,
                LessonId = session.LessonId,
                StudentId = a.StudentId,
                StudentFullName = student?.FullName ?? string.Empty,
                StudentCode = student?.StudentCode ?? string.Empty,
                Status = a.Status,
                FirstScanAt = null,
                LastScanAt = null,
                IsManuallyAdjusted = false,
                InstructorNote = null
            };
        }).ToList();
    }

    public async Task MarkAttendanceByQrAsync(int studentId, int sessionId)
    {
        var session = await _sessionRepository.GetByIdWithLessonAsync(sessionId);

        if (session == null || session.Lesson == null)
            throw new KeyNotFoundException("Session was not found.");

        var isEnrolled = await _enrollmentRepository.ExistsAsync(session.LessonId, studentId);

        if (!isEnrolled)
            throw new InvalidOperationException("Student is not enrolled in this lesson.");

        var attendance = await _attendanceRepository.GetBySessionAndStudentAsync(sessionId, studentId);

        if (attendance == null)
        {
            attendance = new SessionAttendance
            {
                SessionId = sessionId,
                StudentId = studentId,
                Status = AttendanceStatus.Present
            };

            await _attendanceRepository.AddAsync(attendance);
        }
        else
        {
            attendance.Status = AttendanceStatus.Present;
            await _attendanceRepository.UpdateAsync(attendance);
        }
    }

    public async Task UpdateAttendanceAsync(int instructorId, int attendanceId, UpdateAttendanceDto dto)
    {
        var attendance = await _attendanceRepository.GetAsync(
            x => x.Id == attendanceId,
            include: q => q);

        var session = await _sessionRepository.GetByIdWithLessonAsync(attendance.SessionId);

        if (session == null || session.Lesson == null || session.Lesson.InstructorId != instructorId)
            throw new UnauthorizedAccessException("You do not have access to this attendance.");

        attendance.Status = dto.Status;
        await _attendanceRepository.UpdateAsync(attendance);
    }

    public async Task BulkMarkAbsentAsync(int instructorId, int sessionId)
    {
        var session = await _sessionRepository.GetByIdWithLessonAsync(sessionId);

        if (session == null || session.Lesson == null || session.Lesson.InstructorId != instructorId)
            throw new KeyNotFoundException("Session was not found.");

        var enrollments = await _enrollmentRepository.GetByLessonIdAsync(session.LessonId);
        var existingAttendances = await _attendanceRepository.GetBySessionIdAsync(sessionId);

        var existingStudentIds = existingAttendances.Select(x => x.StudentId).ToHashSet();

        foreach (var enrollment in enrollments)
        {
            if (!existingStudentIds.Contains(enrollment.StudentId))
            {
                await _attendanceRepository.AddAsync(new SessionAttendance
                {
                    SessionId = sessionId,
                    StudentId = enrollment.StudentId,
                    Status = AttendanceStatus.Absent
                });
            }
        }
    }
}
