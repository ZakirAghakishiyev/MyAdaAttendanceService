using MyAdaAttendanceService.Core.Entities;

namespace MyAdaAttendanceService.Core.Interfaces;

public interface ILessonEnrollmentRepository : IRepository<LessonEnrollment>
{
    Task<bool> ExistsAsync(int lessonId, Guid studentId);

    Task<List<LessonEnrollment>> GetByStudentIdAsync(Guid studentId);

    Task<List<LessonEnrollment>> GetByLessonIdAsync(int lessonId);
}