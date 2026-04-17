namespace MyAdaAttendanceService.Application.DTOs;

public class UserSyncResultDto
{
    public int ImportedCount { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
    public DateTime SyncedAtUtc { get; set; }
}
