using Microsoft.EntityFrameworkCore;
using MyAdaAttendanceService.Core.Entities;
using MyAdaAttendanceService.Core.Interfaces;

namespace MyAdaAttendanceService.Infrastructure.Repositories;

public class AttendanceActivationRepository : EfCoreRepository<AttendanceActivation>, IAttendanceActivationRepository
{
    public AttendanceActivationRepository(AppDbContext context) : base(context) { }

    public async Task<AttendanceActivation?> GetActiveBySessionIdAsync(int sessionId)
    {
        return await _dbSet
            .Where(x => x.SessionId == sessionId && x.IsActive)
            .OrderByDescending(x => x.StartedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> HasClosedActivationForRoundAsync(int sessionId, byte round)
    {
        return await _dbSet.AnyAsync(x =>
            x.SessionId == sessionId &&
            x.Round == round &&
            !x.IsActive &&
            x.EndedAt != null);
    }
}
