using MyAdaAttendanceService.Core.Entities;

namespace MyAdaAttendanceService.Core.Interfaces;

public interface ICourseRepository : IRepository<Course>
{
    Task<Course?> FindByDepartmentAndCodeAsync(string department, string code, CancellationToken cancellationToken = default);

    Task<bool> HasLessonsAsync(int courseId, CancellationToken cancellationToken = default);
}
