namespace MyAdaAttendanceService.Core;

public static class CrnFormatter
{
    public const int CrnLength = 5;

    public static char PrefixChar(AcademicSemester semester) =>
        semester switch
        {
            AcademicSemester.Fall => '1',
            AcademicSemester.Spring => '2',
            AcademicSemester.Summer => '3',
            _ => throw new ArgumentOutOfRangeException(nameof(semester))
        };

    /// <summary>Formats CRN as one semester digit (1/2/3) plus four-digit sequence (0001–9999).</summary>
    public static string Format(AcademicSemester semester, int sequence)
    {
        if (sequence is < 1 or > 9999)
            throw new ArgumentOutOfRangeException(nameof(sequence), "Sequence must be between 1 and 9999.");
        return $"{PrefixChar(semester)}{sequence:D4}";
    }

}
