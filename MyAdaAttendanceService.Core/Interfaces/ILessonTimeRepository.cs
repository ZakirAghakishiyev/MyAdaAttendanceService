using MyAdaAttendanceService.Core.Entities;

namespace MyAdaAttendanceService.Core.Interfaces;

public interface ILessonTimeRepository : IRepository<LessonTime>
{
    Task<List<LessonTime>> GetByLessonIdAsync(int lessonId);
}