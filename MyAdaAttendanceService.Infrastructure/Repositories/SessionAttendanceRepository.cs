using Microsoft.EntityFrameworkCore;
using MyAdaAttendanceService.Core.Entities;
using MyAdaAttendanceService.Core.Interfaces;

namespace MyAdaAttendanceService.Infrastructure.Repositories;

public class SessionAttendanceRepository : EfCoreRepository<SessionAttendance>, ISessionAttendanceRepository
{
    public SessionAttendanceRepository(AppDbContext context) : base(context) { }

    public async Task<SessionAttendance?> GetBySessionAndStudentAsync(int sessionId, Guid studentId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(x =>
                x.SessionId == sessionId &&
                x.StudentId == studentId);
    }

    public async Task<List<SessionAttendance>> GetBySessionIdAsync(int sessionId)
    {
        return await _dbSet
            .Where(x => x.SessionId == sessionId)
            .ToListAsync();
    }

    public async Task<List<SessionAttendance>> GetStudentAttendanceAsync(Guid studentId, int lessonId)
    {
        return await _dbSet
            .Include(x => x.Session)
            .Where(x =>
                x.StudentId == studentId &&
                x.Session!.LessonId == lessonId)
            .ToListAsync();
    }

    public async Task<int> CountBySessionIdAsync(int sessionId)
    {
        return await _dbSet
            .CountAsync(x => x.SessionId == sessionId);
    }

    public async Task<int> CountBySessionIdAndStatusAsync(int sessionId, AttendanceStatus status)
    {
        return await _dbSet
            .CountAsync(x => x.SessionId == sessionId && x.Status == status);
    }

    public async Task<bool> ExistsAsync(int sessionId, Guid studentId)
    {
        return await _dbSet
            .AnyAsync(x => x.SessionId == sessionId && x.StudentId == studentId);
    }

    public async Task<List<SessionAttendance>> GetManuallyAdjustedBySessionIdAsync(int sessionId)
    {
        return await _dbSet
            .Where(x => x.SessionId == sessionId && x.IsManuallyAdjusted)
            .ToListAsync();
    }
}