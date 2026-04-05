using Microsoft.EntityFrameworkCore;
using MyAdaAttendanceService.Core.Entities;

namespace MyAdaAttendanceService.Core.Interfaces;

public class SessionAttendanceRepository : EfCoreRepository<SessionAttendance>, ISessionAttendanceRepository
{
    public SessionAttendanceRepository(AppDbContext context) : base(context) { }

    public async Task<SessionAttendance?> GetBySessionAndStudentAsync(int sessionId, int studentId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(x => x.SessionId == sessionId && x.StudentId == studentId);
    }

    public async Task<List<SessionAttendance>> GetBySessionIdAsync(int sessionId)
    {
        return await _dbSet
            .Where(x => x.SessionId == sessionId)
            .ToListAsync();
    }

    public async Task<List<SessionAttendance>> GetStudentAttendanceAsync(int studentId, int lessonId)
    {
        return await _dbSet
            .Include(x => x.Session)
            .Where(x => x.StudentId == studentId && x.Session!.LessonId == lessonId)
            .ToListAsync();
    }

    public async Task<int> CountBySessionIdAsync(int sessionId)
    {
        return await _dbSet.CountAsync(x => x.SessionId == sessionId);
    }
}