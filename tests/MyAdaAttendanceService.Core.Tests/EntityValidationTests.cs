using System.ComponentModel.DataAnnotations;
using MyAdaAttendanceService.Core.Entities;

namespace MyAdaAttendanceService.Core.Tests;

public class EntityValidationTests
{
    private static List<ValidationResult> Validate(object instance)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(instance);
        Validator.TryValidateObject(instance, context, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void Lesson_FailsValidation_WhenNameIsMissing()
    {
        var lesson = new Lesson
        {
            InstructorId = 1,
            RoomId = 1,
            Semester = "Spring",
            CRN = "1",
            Name = "",
            Type = "Core",
            Department = "CS",
            Code = "CSE201"
        };

        var results = Validate(lesson);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Lesson.Name)));
    }

    [Fact]
    public void Lesson_FailsValidation_WhenInstructorIdIsZero()
    {
        var lesson = new Lesson
        {
            InstructorId = 0,
            RoomId = 1,
            Semester = "Spring",
            CRN = "1",
            Name = "Algorithms",
            Type = "Core",
            Department = "CS",
            Code = "CSE201"
        };

        var results = Validate(lesson);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Lesson.InstructorId)));
    }

    [Fact]
    public void Lesson_FailsValidation_WhenNameTooLong()
    {
        var lesson = new Lesson
        {
            InstructorId = 1,
            RoomId = 1,
            Semester = "Spring",
            CRN = "1",
            Name = new string('a', 300),
            Type = "Core",
            Department = "CS",
            Code = "CSE201"
        };

        var results = Validate(lesson);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Lesson.Name)));
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
            StudentId = 1,
            ActivationId = 0,
            TokenJti = new string('x', 200),
            ScannedAt = DateTime.UtcNow,
            Accepted = false
        };

        var results = Validate(log);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AttendanceScanLog.TokenJti)));
    }
}

