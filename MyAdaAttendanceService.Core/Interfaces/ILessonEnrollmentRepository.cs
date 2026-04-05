using MyAdaAttendanceService.Core.Entities;

namespace MyAdaAttendanceService.Core.Interfaces;

public interface ILessonEnrollmentRepository : IRepository<LessonEnrollment>
{
    Task<bool> ExistsAsync(int lessonId, int studentId);

    Task<List<LessonEnrollment>> GetByStudentIdAsync(int studentId);

    Task<List<LessonEnrollment>> GetByLessonIdAsync(int lessonId);
}