using Microsoft.EntityFrameworkCore;
using MyAdaAttendanceService.Core.Entities;
using MyAdaAttendanceService.Core.Interfaces;

namespace MyAdaAttendanceService.Infrastructure.Repositories;

public class LessonSessionRepository : EfCoreRepository<LessonSession>, ILessonSessionRepository
{
    public LessonSessionRepository(AppDbContext context) : base(context) { }

    public async Task<List<LessonSession>> GetByLessonIdAsync(int lessonId)
    {
        return await _dbSet
            .Where(x => x.LessonId == lessonId)
            .OrderBy(x => x.Date)
            .ThenBy(x => x.StartTime)
            .ToListAsync();
    }

    public async Task<LessonSession?> GetByIdWithLessonAsync(int sessionId)
    {
        return await _dbSet
            .Include(x => x.Lesson!)
            .ThenInclude(l => l.Course)
            .FirstOrDefaultAsync(x => x.Id == sessionId);
    }

    public async Task<LessonSession?> GetByIdWithAttendancesAsync(int sessionId)
    {
        return await _dbSet
            .Include(x => x.Attendances)
            .FirstOrDefaultAsync(x => x.Id == sessionId);
    }

    public async Task<LessonSession?> GetInstructorSessionAsync(Guid instructorId, int sessionId)
    {
        return await _dbSet
            .Include(x => x.Lesson!)
            .ThenInclude(l => l.Course)
            .FirstOrDefaultAsync(x =>
                x.Id == sessionId &&
                x.Lesson != null &&
                x.Lesson.InstructorId == instructorId);
    }

    public async Task<List<LessonSession>> GetUpcomingSessionsAsync(int lessonId, DateTime now)
    {
        return await _dbSet
            .Where(x =>
                x.LessonId == lessonId &&
                x.Date.ToDateTime(x.StartTime) > now)
            .ToListAsync();
    }

    public async Task<List<LessonSession>> GetPastSessionsAsync(int lessonId, DateTime now)
    {
        return await _dbSet
            .Where(x =>
                x.LessonId == lessonId &&
                x.Date.ToDateTime(x.EndTime) < now)
            .ToListAsync();
    }

    public async Task<LessonSession?> GetActiveAttendanceSessionAsync(int lessonId)
    {
        return await _dbSet
            .Where(x => x.LessonId == lessonId && x.IsAttendanceActive)
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.StartTime)
            .FirstOrDefaultAsync();
    }

    public async Task AddRangeAsync(IEnumerable<LessonSession> sessions, CancellationToken cancellationToken = default)
    {
        var list = sessions as IList<LessonSession> ?? sessions.ToList();
        if (list.Count == 0)
            return;

        await _dbSet.AddRangeAsync(list, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}