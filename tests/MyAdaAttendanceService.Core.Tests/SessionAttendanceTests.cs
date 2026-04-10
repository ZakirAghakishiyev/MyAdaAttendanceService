using MyAdaAttendanceService.Core.Entities;

namespace MyAdaAttendanceService.Core.Tests;

public class SessionAttendanceTests
{
    [Fact]
    public void NewSessionAttendance_DefaultsMarkedSourceToQr()
    {
        var attendance = new SessionAttendance();

        Assert.Equal(AttendanceMarkedSource.QR, attendance.MarkedSource);
    }

    [Fact]
    public void NewLesson_InitializesNavigationCollections()
    {
        var lesson = new Lesson();

        Assert.NotNull(lesson.Enrollments);
        Assert.NotNull(lesson.Sessions);
        Assert.NotNull(lesson.LessonTimes);
        Assert.Empty(lesson.Enrollments);
        Assert.Empty(lesson.Sessions);
        Assert.Empty(lesson.LessonTimes);
    }
}
