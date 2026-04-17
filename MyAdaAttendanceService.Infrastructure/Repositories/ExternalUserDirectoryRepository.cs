using Microsoft.EntityFrameworkCore;
using MyAdaAttendanceService.Core.Entities;
using MyAdaAttendanceService.Core.Interfaces;

namespace MyAdaAttendanceService.Infrastructure.Repositories;

public class ExternalUserDirectoryRepository : EfCoreRepository<ExternalUserDirectoryEntry>, IExternalUserDirectoryRepository
{
    public ExternalUserDirectoryRepository(AppDbContext context) : base(context)
    {
    }

    public Task<ExternalUserDirectoryEntry?> GetByUserIdAsync(Guid userId)
    {
        return _dbSet.FirstOrDefaultAsync(x => x.UserId == userId);
    }

    public Task<List<ExternalUserDirectoryEntry>> GetByRoleAsync(string role)
    {
        var normalizedRole = role.Trim().ToLowerInvariant();
        return _dbSet
            .Where(x => x.Role == normalizedRole)
            .OrderBy(x => x.FirstName)
            .ThenBy(x => x.LastName)
            .ToListAsync();
    }

    public Task<List<ExternalUserDirectoryEntry>> GetByUserIdsAsync(IEnumerable<Guid> userIds)
    {
        var ids = userIds.Where(x => x != Guid.Empty).Distinct().ToList();
        if (ids.Count == 0)
            return Task.FromResult(new List<ExternalUserDirectoryEntry>());

        return _dbSet
            .Where(x => ids.Contains(x.UserId))
            .ToListAsync();
    }

    public async Task UpsertRangeAsync(IEnumerable<ExternalUserDirectoryEntry> entries, CancellationToken cancellationToken = default)
    {
        var incoming = entries
            .Where(x => x.UserId != Guid.Empty)
            .GroupBy(x => x.UserId)
            .Select(g => g.Last())
            .ToList();

        if (incoming.Count == 0)
            return;

        var userIds = incoming.Select(x => x.UserId).ToList();
        var existing = await _dbSet
            .Where(x => userIds.Contains(x.UserId))
            .ToDictionaryAsync(x => x.UserId, cancellationToken);

        foreach (var entry in incoming)
        {
            if (existing.TryGetValue(entry.UserId, out var current))
            {
                current.Role = entry.Role;
                current.UserName = entry.UserName;
                current.Email = entry.Email;
                current.FirstName = entry.FirstName;
                current.LastName = entry.LastName;
                current.PhoneNumber = entry.PhoneNumber;
                current.UserType = entry.UserType;
                current.Status = entry.Status;
                current.SyncedAtUtc = entry.SyncedAtUtc;
            }
            else
            {
                await _dbSet.AddAsync(entry, cancellationToken);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
