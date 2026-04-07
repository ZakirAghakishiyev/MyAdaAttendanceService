using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Application.Services.Interfaces;
using MyAdaAttendanceService.Core.Entities;
using MyAdaAttendanceService.Core.Interfaces;
using MyAdaAttendanceService.Infrastructure.Repositories;

namespace MyAdaAttendanceService.Application.Services;

public class StudentAttendanceService : IStudentAttendanceService
{
    private readonly ILessonRepository _lessonRepository;
    private readonly ISessionAttendanceRepository _attendanceRepository;

    public StudentAttendanceService(
        ILessonRepository lessonRepository,
        ISessionAttendanceRepository attendanceRepository)
    {
        _lessonRepository = lessonRepository;
        _attendanceRepository = attendanceRepository;
    }

    public async Task<IEnumerable<StudentLessonDto>> GetMyLessonsAsync(int studentId)
    {
        var lessons = await _lessonRepository.GetStudentLessonsAsync(studentId);
        var result = new List<StudentLessonDto>();

        foreach (var lesson in lessons)
        {
            var detailedLesson = await _lessonRepository.GetByIdWithDetailsAsync(lesson.Id);
            var attendances = await _attendanceRepository.GetStudentAttendanceAsync(studentId, lesson.Id);

            result.Add(new StudentLessonDto
            {
                LessonId = lesson.Id,
                LessonName = lesson.Name,
                LessonCode = lesson.Code,
                TotalSessions = detailedLesson?.Sessions.Count ?? 0,
                PresentCount = attendances.Count(x => x.Status == AttendanceStatus.Present),
                LateCount = attendances.Count(x => x.Status == AttendanceStatus.Late),
                AbsentCount = attendances.Count(x => x.Status == AttendanceStatus.Absent),
                ExcusedCount = attendances.Count(x => x.Status == AttendanceStatus.Excused)
            });
        }

        return result;
    }

    public async Task<IEnumerable<StudentAttendanceDto>> GetMyAttendanceAsync(int studentId, int lessonId)
    {
        var lesson = await _lessonRepository.GetByIdWithDetailsAsync(lessonId);

        if (lesson == null)
            throw new KeyNotFoundException("Lesson was not found.");

        var attendances = await _attendanceRepository.GetStudentAttendanceAsync(studentId, lessonId);

        return attendances
            .Where(x => x.Session != null)
            .OrderBy(x => x.Session!.Date)
            .ThenBy(x => x.Session!.StartTime)
            .Select(x => new StudentAttendanceDto
            {
                AttendanceId = x.Id,
                SessionId = x.SessionId,
                SessionStartTime = x.Session!.Date.ToDateTime(x.Session.StartTime),
                SessionEndTime = x.Session.Date.ToDateTime(x.Session.EndTime),
                LessonName = lesson.Name,
                LessonCode = lesson.Code,
                Status = x.Status.ToString(),
                FirstScanAt = null,
                InstructorNote = null
            })
            .ToList();
    }

    public async Task<IEnumerable<StudentAttendanceDto>> GetMyAttendanceByLessonAsync(int studentId, int lessonId)
    {
        var isEnrolled = await _lessonRepository.ExistsAsync(lessonId, studentId);
        if (!isEnrolled)
            throw new UnauthorizedAccessException("Student is not enrolled in this lesson.");

        var attendances = await _attendanceRepository.GetStudentAttendanceAsync(studentId, lessonId);

        var result = attendances
            .OrderBy(x => x.Session!.Date)
            .ThenBy(x => x.Session!.StartTime)
            .Select(x => new StudentAttendanceDto
            {
                AttendanceId = x.Id,
                SessionId = x.SessionId,
                SessionStartTime = x.Session != null
                    ? x.Session.Date.ToDateTime(x.Session.StartTime)
                    : default,
                SessionEndTime = x.Session != null
                    ? x.Session.Date.ToDateTime(x.Session.EndTime)
                    : default,
                LessonName = x.Session?.Lesson?.Name ?? string.Empty,
                LessonCode = x.Session?.Lesson?.Code ?? string.Empty,
                Status = x.Status.ToString(),
                FirstScanAt = x.FirstScanAt,
                LastScanAt = x.LastScanAt,
                IsManuallyAdjusted = x.IsManuallyAdjusted,
                InstructorNote = x.InstructorNote
            })
            .ToList();

        return result;
    }
}