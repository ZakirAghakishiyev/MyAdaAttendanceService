using MyAdaAttendanceService.Application.DTOs;

namespace MyAdaAttendanceService.Application.Services.Interfaces;

public interface IEnrollmentService
{
    Task<bool> IsStudentEnrolledAsync(Guid studentId, int lessonId);

    Task<IEnumerable<StudentDto>> GetStudentsByLessonAsync(int lessonId);

    Task<IEnumerable<EnrollmentDto>> GetEnrollmentsByLessonAsync(int lessonId);

    Task<EnrollmentDto> CreateEnrollmentAsync(int lessonId, CreateEnrollmentDto dto);
}
