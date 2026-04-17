namespace MyAdaAttendanceService.Core;

/// <summary>Read model for scheduling integrations: lesson id, instructor user id, enrolled headcount, weekly session count, and course code/title.</summary>
public sealed class LessonSchedulingRow
{
    public int LessonId { get; init; }

    public Guid InstructorUserId { get; init; }

    /// <summary>Students enrolled in this service (<see cref="LessonEnrollment"/> rows). Zero until enrollments are imported or recorded.</summary>
    public int Enrollment { get; init; }

    /// <summary>Maximum seats for the section (same as lesson max capacity / pipe <c>availableSeats</c>).</summary>
    public int MaxCapacity { get; init; }

    public int TimesPerWeek { get; init; }

    public string CourseCode { get; init; } = string.Empty;

    public string CourseTitle { get; init; } = string.Empty;
}
