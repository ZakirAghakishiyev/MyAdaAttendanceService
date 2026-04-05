using MyAdaAttendanceService.Application.DTOs;

namespace MyAdaAttendanceService.Application.Services.Interfaces;

public interface IEnrollmentService
{
    Task<bool> IsStudentEnrolledAsync(int studentId, int lessonId);

    Task<IEnumerable<StudentDto>> GetStudentsByLessonAsync(int lessonId);
}
