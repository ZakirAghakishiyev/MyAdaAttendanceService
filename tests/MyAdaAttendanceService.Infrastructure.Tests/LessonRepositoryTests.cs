using Microsoft.EntityFrameworkCore;
using MyAdaAttendanceService.Core.Entities;
using MyAdaAttendanceService.Infrastructure;
using MyAdaAttendanceService.Infrastructure.Repositories;

namespace MyAdaAttendanceService.Infrastructure.Tests;

public class LessonRepositoryTests
{
    [Fact]
    public async Task GetByInstructorIdAsync_ReturnsOnlyRequestedInstructorLessons()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);
        context.Lessons.AddRange(
            new Lesson { InstructorId = 7, Name = "Math", Code = "MTH101", Semester = "Spring", CRN = "1", Type = "Core", Department = "Math", RoomId = 1, Credits = 3, TimesPerWeek = 2, Capacity = 30 },
            new Lesson { InstructorId = 9, Name = "Physics", Code = "PHY101", Semester = "Spring", CRN = "2", Type = "Core", Department = "Physics", RoomId = 2, Credits = 3, TimesPerWeek = 2, Capacity = 30 });
        await context.SaveChangesAsync();

        var repository = new LessonRepository(context);
        var result = await repository.GetByInstructorIdAsync(7);

        Assert.Single(result);
        Assert.Equal(7, result[0].InstructorId);
    }

    [Fact]
    public async Task GetByIdWithDetailsAsync_ReturnsLessonWithSessionsAndEnrollments()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);
        var lesson = new Lesson
        {
            InstructorId = 4,
            Name = "Networks",
            Code = "CSE320",
            Semester = "Spring",
            CRN = "10",
            Type = "Core",
            Department = "CS",
            RoomId = 11,
            Credits = 3,
            TimesPerWeek = 2,
            Capacity = 35
        };
        context.Lessons.Add(lesson);
        await context.SaveChangesAsync();

        context.LessonSessions.Add(new LessonSession
        {
            LessonId = lesson.Id,
            Date = new DateOnly(2026, 4, 10),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0)
        });
        context.LessonEnrollments.Add(new LessonEnrollment { LessonId = lesson.Id, StudentId = 100 });
        await context.SaveChangesAsync();

        var repository = new LessonRepository(context);
        var result = await repository.GetByIdWithDetailsAsync(lesson.Id);

        Assert.NotNull(result);
        Assert.Single(result!.Sessions);
        Assert.Single(result.Enrollments);
    }

    [Fact]
    public async Task ExistsAsync_ReturnsFalse_WhenStudentNotEnrolled()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);
        context.Lessons.Add(new Lesson
        {
            InstructorId = 1,
            Name = "AI",
            Code = "CSE400",
            Semester = "Spring",
            CRN = "3",
            Type = "Elective",
            Department = "CS",
            RoomId = 1,
            Credits = 3,
            TimesPerWeek = 2,
            Capacity = 20
        });
        await context.SaveChangesAsync();

        var lessonId = context.Lessons.Single().Id;
        var repository = new LessonRepository(context);
        var exists = await repository.ExistsAsync(999, lessonId);

        Assert.False(exists);
    }
}
