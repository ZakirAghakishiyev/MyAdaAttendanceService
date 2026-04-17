namespace MyAdaAttendanceService.Application.DTOs;

/// <summary>One weekly meeting time (same wall-clock time each matching weekday in the date range).</summary>
public class WeeklySessionSlotDto
{
    /// <summary>Day of week in the server's calendar (e.g. <see cref="DayOfWeek.Monday"/>).</summary>
    public DayOfWeek DayOfWeek { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }
}
