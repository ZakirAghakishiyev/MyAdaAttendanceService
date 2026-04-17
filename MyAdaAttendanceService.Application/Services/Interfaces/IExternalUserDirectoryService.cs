using MyAdaAttendanceService.Application.DTOs;

namespace MyAdaAttendanceService.Application.Services.Interfaces;

public interface IExternalUserDirectoryService
{
    Task<UserSyncResultDto> SyncUsersAsync(IEnumerable<string>? roles = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExternalUserDirectoryDto>> GetUsersByRoleAsync(string role, CancellationToken cancellationToken = default);
    Task<ExternalUserDirectoryDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, ExternalUserDirectoryDto>> GetUsersByIdsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default);
    Task EnsureUserExistsInRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default);
}
