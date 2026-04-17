using Microsoft.EntityFrameworkCore;
using MyAdaAttendanceService.Core.Entities;
using MyAdaAttendanceService.Core.Interfaces;

namespace MyAdaAttendanceService.Infrastructure.Repositories;

public class AttendanceScanLogRepository : EfCoreRepository<AttendanceScanLog>, IAttendanceScanLogRepository
{
    public AttendanceScanLogRepository(AppDbContext context) : base(context) { }

    public async Task<bool> ExistsAcceptedByTokenAsync(int sessionId, Guid studentId, string tokenJti)
    {
        return await _dbSet.AnyAsync(x =>
            x.SessionId == sessionId &&
            x.StudentId == studentId &&
            x.TokenJti == tokenJti &&
            x.Accepted);
    }

    public async Task<int> CountAcceptedScansAsync(int sessionId, Guid studentId, int activationId)
    {
        return await _dbSet.CountAsync(x =>
            x.SessionId == sessionId &&
            x.StudentId == studentId &&
            x.ActivationId == activationId &&
            x.Accepted);
    }

    public async Task<List<AttendanceScanLog>> GetAcceptedBySessionAndActivationAsync(int sessionId, int activationId)
    {
        return await _dbSet
            .Where(x => x.SessionId == sessionId && x.ActivationId == activationId && x.Accepted)
            .ToListAsync();
    }
}
