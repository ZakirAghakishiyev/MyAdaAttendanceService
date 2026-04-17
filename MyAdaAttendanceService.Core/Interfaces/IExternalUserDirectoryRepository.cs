using MyAdaAttendanceService.Core.Entities;

namespace MyAdaAttendanceService.Core.Interfaces;

public interface IExternalUserDirectoryRepository : IRepository<ExternalUserDirectoryEntry>
{
    Task<ExternalUserDirectoryEntry?> GetByUserIdAsync(Guid userId);
    Task<List<ExternalUserDirectoryEntry>> GetByRoleAsync(string role);
    Task<List<ExternalUserDirectoryEntry>> GetByUserIdsAsync(IEnumerable<Guid> userIds);
    Task UpsertRangeAsync(IEnumerable<ExternalUserDirectoryEntry> entries, CancellationToken cancellationToken = default);
}
