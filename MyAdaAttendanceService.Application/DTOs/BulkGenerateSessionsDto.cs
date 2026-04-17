namespace MyAdaAttendanceService.Application.DTOs;

/// <summary>Generate class sessions for a lesson between two dates on the given weekly pattern.</summary>
public class BulkGenerateSessionsDto
{
    /// <summary>Inclusive start date.</summary>
    public DateOnly FromDate { get; set; }

    /// <summary>Inclusive end date.</summary>
    public DateOnly ToDate { get; set; }

    /// <summary>At least one slot; duplicate (day + start + end) entries are ignored.</summary>
    public List<WeeklySessionSlotDto> WeeklySlots { get; set; } = new();

    /// <summary>Optional topic applied to every generated session.</summary>
    public string? Topic { get; set; }
}
