using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Core.Entities;
using MyAdaAttendanceService.Core.Interfaces;

namespace MyAdaAttendanceService.Application.Services;

public class StudentAttendanceService : IStudentAttendanceService
{
    private readonly ILessonRepository _lessonRepository;
    private readonly ISessionAttendanceRepository _attendanceRepository;
    private readonly ILessonSessionRepository _sessionRepository;

    public StudentAttendanceService(
        ILessonRepository lessonRepository,
        ISessionAttendanceRepository attendanceRepository,
        ILessonSessionRepository sessionRepository)
    {
        _lessonRepository = lessonRepository;
        _attendanceRepository = attendanceRepository;
        _sessionRepository = sessionRepository;
    }

    public async Task<IEnumerable<StudentLessonDto>> GetMyLessonsAsync(int studentId)
    {
        var lessons = await _lessonRepository.GetStudentLessonsAsync(studentId);
        var result = new List<StudentLessonDto>();

        foreach (var lesson in lessons)
        {
            var sessions = await _sessionRepository.GetByLessonIdAsync(lesson.Id);
            var sessionIds = sessions.Select(s => s.Id).ToList();

            var attendances = await _attendanceRepository.GetAllAsync(
                predicate: a => a.StudentId == studentId && sessionIds.Contains(a.SessionId));

            result.Add(new StudentLessonDto
            {
                LessonId = lesson.Id,
                LessonName = lesson.Name,
                LessonCode = lesson.Code,
                TotalSessions = sessions.Count,
                PresentCount = attendances.Count(a => a.Status == AttendanceStatus.Present),
                LateCount = attendances.Count(a => a.Status == AttendanceStatus.Late),
                AbsentCount = attendances.Count(a => a.Status == AttendanceStatus.Absent),
                ExcusedCount = attendances.Count(a => a.Status == AttendanceStatus.Excused)
            });
        }

        return result;
    }

    public async Task<IEnumerable<StudentAttendanceDto>> GetMyAttendanceByLessonAsync(int studentId, int lessonId)
    {
        var lesson = await _lessonRepository.GetByIdAsync(lessonId);
        var records = await _attendanceRepository.GetStudentAttendanceAsync(studentId, lessonId);

        return records.Select(r => new StudentAttendanceDto
        {
            AttendanceId = r.Id,
            SessionId = r.SessionId,
            SessionStartTime = r.Session!.Date.ToDateTime(r.Session.StartTime),
            SessionEndTime = r.Session.Date.ToDateTime(r.Session.EndTime),
            LessonName = lesson.Name,
            LessonCode = lesson.Code,
            Status = r.Status.ToString(),
            FirstScanAt = r.FirstScanAt,
            LastScanAt = r.LastScanAt,
            IsManuallyAdjusted = r.IsManuallyAdjusted,
            InstructorNote = r.InstructorNote
        });
    }
}
