using MyAdaAttendanceService.Application.DTOs;

namespace MyAdaAttendanceService.Application.Services.Interfaces;

public interface IStudentAttendanceService
{
    Task<IEnumerable<StudentLessonDto>> GetMyLessonsAsync(int studentId);

    Task<IEnumerable<StudentAttendanceDto>> GetMyAttendanceAsync(int studentId, int lessonId);
}
