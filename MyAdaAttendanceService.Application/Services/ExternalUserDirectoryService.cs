using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Application.Services.Interfaces;
using MyAdaAttendanceService.Core.Entities;
using MyAdaAttendanceService.Core.Interfaces;

namespace MyAdaAttendanceService.Application.Services;

public class ExternalUserDirectoryService : IExternalUserDirectoryService
{
    private static readonly string[] DefaultRoles = ["student", "instructor"];

    private readonly IAuthUserClient _authUserClient;
    private readonly IExternalUserDirectoryRepository _directoryRepository;

    public ExternalUserDirectoryService(
        IAuthUserClient authUserClient,
        IExternalUserDirectoryRepository directoryRepository)
    {
        _authUserClient = authUserClient;
        _directoryRepository = directoryRepository;
    }

    public async Task<UserSyncResultDto> SyncUsersAsync(IEnumerable<string>? roles = null, CancellationToken cancellationToken = default)
    {
        var normalizedRoles = (roles ?? DefaultRoles)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToLowerInvariant())
            .Distinct()
            .ToArray();

        var syncedAtUtc = DateTime.UtcNow;
        var imported = new List<ExternalUserDirectoryEntry>();

        foreach (var role in normalizedRoles)
        {
            var users = await _authUserClient.GetUsersByRoleAsync(role, cancellationToken);
            imported.AddRange(users.Select(user => new ExternalUserDirectoryEntry
            {
                UserId = user.Id,
                Role = role,
                UserName = user.UserName,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                UserType = user.UserType,
                Status = user.Status,
                SyncedAtUtc = syncedAtUtc
            }));
        }

        await _directoryRepository.UpsertRangeAsync(imported, cancellationToken);

        return new UserSyncResultDto
        {
            ImportedCount = imported.Select(x => x.UserId).Distinct().Count(),
            Roles = normalizedRoles,
            SyncedAtUtc = syncedAtUtc
        };
    }

    public async Task<IReadOnlyList<ExternalUserDirectoryDto>> GetUsersByRoleAsync(string role, CancellationToken cancellationToken = default)
    {
        var entries = await _directoryRepository.GetByRoleAsync(role);
        return entries.Select(Map).ToList();
    }

    public async Task<ExternalUserDirectoryDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var entry = await _directoryRepository.GetByUserIdAsync(userId);
        return entry is null ? null : Map(entry);
    }

    public async Task<IReadOnlyDictionary<Guid, ExternalUserDirectoryDto>> GetUsersByIdsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default)
    {
        var entries = await _directoryRepository.GetByUserIdsAsync(userIds);
        return entries
            .Select(Map)
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.First());
    }

    public async Task EnsureUserExistsInRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User id is required.");

        var user = await GetUserByIdAsync(userId, cancellationToken);
        if (user is not null)
            return;

        // Temporary ID-only mode: do not call AuthService here.
        // If user is not present in local directory, create a minimal placeholder row.
        var normalizedRole = string.IsNullOrWhiteSpace(role)
            ? "unknown"
            : role.Trim().ToLowerInvariant();

        await _directoryRepository.UpsertRangeAsync(
        [
            new ExternalUserDirectoryEntry
            {
                UserId = userId,
                Role = normalizedRole,
                UserName = userId.ToString("N"),
                Email = string.Empty,
                FirstName = string.Empty,
                LastName = string.Empty,
                PhoneNumber = null,
                UserType = null,
                Status = "Unknown",
                SyncedAtUtc = DateTime.UtcNow
            }
        ], cancellationToken);
    }

    private static ExternalUserDirectoryDto Map(ExternalUserDirectoryEntry entry)
    {
        var displayName = $"{entry.FirstName} {entry.LastName}".Trim();
        if (string.IsNullOrWhiteSpace(displayName))
            displayName = entry.UserName;

        return new ExternalUserDirectoryDto
        {
            UserId = entry.UserId,
            Role = entry.Role,
            UserName = entry.UserName,
            Email = entry.Email,
            FirstName = entry.FirstName,
            LastName = entry.LastName,
            DisplayName = displayName,
            PhoneNumber = entry.PhoneNumber,
            UserType = entry.UserType,
            Status = entry.Status,
            SyncedAtUtc = entry.SyncedAtUtc
        };
    }
}
