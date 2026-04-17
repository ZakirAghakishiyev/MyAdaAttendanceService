using MyAdaAttendanceService.Core;
using MyAdaAttendanceService.Core.Entities;

namespace MyAdaAttendanceService.Core.Interfaces;

public interface ILessonRepository : IRepository<Lesson>
{
    Task<List<Lesson>> GetByInstructorIdAsync(Guid instructorId);

    Task<Lesson?> GetByIdWithDetailsAsync(int lessonId);

    Task<Lesson?> GetByIdWithCourseAsync(int lessonId);

    Task<List<Lesson>> GetStudentLessonsAsync(Guid studentId);

    Task<bool> ExistsAsync(Guid studentId, int lessonId);

    Task<List<Lesson>> GetByCourseIdAsync(int courseId);

    Task<List<Lesson>> GetByAcademicTermAsync(int academicYear, AcademicSemester semester);

    Task<Lesson> AddWithGeneratedCrnAsync(Lesson lesson);

    Task UpdateLessonWithAutoCrnAsync(
        Lesson lesson,
        int newAcademicYear,
        AcademicSemester newSemester,
        int courseId,
        bool termChanged,
        Guid instructorId,
        int roomId,
        int maxCapacity);

    Task<IReadOnlyList<LessonSchedulingRow>> GetLessonSchedulingRowsAsync();
}