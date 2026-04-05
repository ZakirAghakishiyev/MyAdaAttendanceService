using MyAdaAttendanceService.Core.Entities;

namespace MyAdaAttendanceService.Core.Interfaces;

public interface ILessonRepository : IRepository<Lesson>
{
    Task<List<Lesson>> GetByInstructorIdAsync(int instructorId);

    Task<Lesson?> GetByIdWithDetailsAsync(int lessonId);

    Task<List<Lesson>> GetStudentLessonsAsync(int studentId);
}