namespace MyAdaAttendanceService.Application.DTOs;

public class BulkGenerateSessionsResponseDto
{
    public int CreatedCount { get; set; }

    /// <summary>Sessions skipped because the same date and start/end time already existed.</summary>
    public int SkippedDuplicateCount { get; set; }

    public IReadOnlyList<SessionDto> CreatedSessions { get; set; } = Array.Empty<SessionDto>();
}
