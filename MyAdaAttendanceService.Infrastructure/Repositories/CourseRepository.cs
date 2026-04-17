using Microsoft.EntityFrameworkCore;
using MyAdaAttendanceService.Core.Entities;
using MyAdaAttendanceService.Core.Interfaces;

namespace MyAdaAttendanceService.Infrastructure.Repositories;

public class CourseRepository : EfCoreRepository<Course>, ICourseRepository
{
    public CourseRepository(AppDbContext context) : base(context) { }

    public async Task<Course?> FindByDepartmentAndCodeAsync(string department, string code, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.Department == department && c.Code == code,
                cancellationToken);
    }

    public async Task<bool> HasLessonsAsync(int courseId, CancellationToken cancellationToken = default)
    {
        return await _context.Lessons.AnyAsync(l => l.CourseId == courseId, cancellationToken);
    }
}
