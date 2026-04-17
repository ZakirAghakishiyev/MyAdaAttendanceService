using System.ComponentModel.DataAnnotations;
using MyAdaAttendanceService.Core;
using MyAdaAttendanceService.Core.Entities;

namespace MyAdaAttendanceService.Core.Tests;

public class EntityValidationTests
{
    private static readonly Guid ValidUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static List<ValidationResult> Validate(object instance)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(instance);
        Validator.TryValidateObject(instance, context, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void Lesson_FailsValidation_WhenCrnIsMissing()
    {
        var lesson = new Lesson
        {
            InstructorId = ValidUserId,
            RoomId = 1,
            CourseId = 1,
            AcademicYear = 2026,
            Semester = AcademicSemester.Spring,
            CRN = ""
        };

        var results = Validate(lesson);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Lesson.CRN)));
    }

    [Fact]
    public void Lesson_FailsValidation_WhenInstructorIdIsEmpty()
    {
        var lesson = new Lesson
        {
            InstructorId = Guid.Empty,
            RoomId = 1,
            CourseId = 1,
            AcademicYear = 2026,
            Semester = AcademicSemester.Spring,
            CRN = "20001"
        };

        var results = Validate(lesson);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Lesson.InstructorId)));
    }

    [Fact]
    public void Lesson_FailsValidation_WhenCrnTooLong()
    {
        var lesson = new Lesson
        {
            InstructorId = ValidUserId,
            RoomId = 1,
            CourseId = 1,
            AcademicYear = 2026,
            Semester = AcademicSemester.Spring,
            CRN = "123456"
        };

        var results = Validate(lesson);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Lesson.CRN)));
    }

    [Fact]
    public void Timeslot_FailsValidation_WhenDayIsMissing()
    {
        var timeslot = new Timeslot { Day = "" };
        var results = Validate(timeslot);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Timeslot.Day)));
    }

    [Fact]
    public void AttendanceScanLog_FailsValidation_WhenTokenJtiTooLong()
    {
        var log = new AttendanceScanLog
        {
            SessionId = 1,
            StudentId = ValidUserId,
            ActivationId = 0,
            TokenJti = new string('x', 200),
            ScannedAt = DateTime.UtcNow,
            Accepted = false
        };

        var results = Validate(log);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AttendanceScanLog.TokenJti)));
    }
}
