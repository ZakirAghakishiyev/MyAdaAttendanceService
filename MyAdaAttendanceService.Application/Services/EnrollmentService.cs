using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Application.Services.Interfaces;
using MyAdaAttendanceService.Core.Interfaces;

namespace MyAdaAttendanceService.Application.Services;

public class EnrollmentService : IEnrollmentService
{
    private readonly ILessonEnrollmentRepository _enrollmentRepository;

    public EnrollmentService(ILessonEnrollmentRepository enrollmentRepository)
    {
        _enrollmentRepository = enrollmentRepository;
    }

    public async Task<bool> IsStudentEnrolledAsync(int studentId, int lessonId)
    {
        return await _enrollmentRepository.ExistsAsync(lessonId, studentId);
    }

    public async Task<IEnumerable<StudentDto>> GetStudentsByLessonAsync(int lessonId)
    {
        var enrollments = await _enrollmentRepository.GetByLessonIdAsync(lessonId);

        return enrollments.Select(e => new StudentDto
        {
            Id = e.StudentId
        });
    }

    public async Task<IEnumerable<EnrollmentDto>> GetEnrollmentsByLessonAsync(int lessonId)
    {
        var enrollments = await _enrollmentRepository.GetByLessonIdAsync(lessonId);

        return enrollments.Select(e => new EnrollmentDto
        {
            Id = e.Id,
            LessonId = e.LessonId,
            StudentId = e.StudentId
        });
    }
}
