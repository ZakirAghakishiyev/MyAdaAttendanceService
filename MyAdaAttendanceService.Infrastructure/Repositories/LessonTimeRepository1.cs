using Microsoft.EntityFrameworkCore;
using MyAdaAttendanceService.Core.Entities;
using MyAdaAttendanceService.Core.Interfaces;

namespace MyAdaAttendanceService.Infrastructure.Repositories;

public class LessonTimeRepository : EfCoreRepository<LessonTime>, ILessonTimeRepository
{
    public LessonTimeRepository(AppDbContext context) : base(context) { }

    public async Task<List<LessonTime>> GetByLessonIdAsync(int lessonId)
    {
        return await _dbSet
            .Include(x => x.Timeslot)
            .Where(x => x.LessonId == lessonId)
            .ToListAsync();
    }
}