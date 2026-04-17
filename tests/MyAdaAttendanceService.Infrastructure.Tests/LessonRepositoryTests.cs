using Microsoft.EntityFrameworkCore;
using MyAdaAttendanceService.Core;
using MyAdaAttendanceService.Core.Entities;
using MyAdaAttendanceService.Infrastructure;
using MyAdaAttendanceService.Infrastructure.Repositories;

namespace MyAdaAttendanceService.Infrastructure.Tests;

public class LessonRepositoryTests
{
    private static readonly Guid InstructorA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid InstructorB = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid StudentA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid StudentB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid StudentC = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid MissingStudent = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    [Fact]
    public async Task GetByInstructorIdAsync_ReturnsOnlyRequestedInstructorLessons()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);
        var c1 = new Course
        {
            Name = "Math 101",
            Department = "Math",
            Code = "MTH101",
            Credits = 3,
            TimesPerWeek = 2
        };
        var c2 = new Course
        {
            Name = "Physics 101",
            Department = "Physics",
            Code = "PHY101",
            Credits = 3,
            TimesPerWeek = 2
        };
        context.Courses.AddRange(c1, c2);
        await context.SaveChangesAsync();

        context.Lessons.AddRange(
            new Lesson
            {
                InstructorId = InstructorA,
                CourseId = c1.Id,
                AcademicYear = 2026,
                Semester = AcademicSemester.Spring,
                CRN = "20001",
                RoomId = 1,
                MaxCapacity = 30
            },
            new Lesson
            {
                InstructorId = InstructorB,
                CourseId = c2.Id,
                AcademicYear = 2026,
                Semester = AcademicSemester.Spring,
                CRN = "20002",
                RoomId = 2,
                MaxCapacity = 30
            });
        await context.SaveChangesAsync();

        var repository = new LessonRepository(context);
        var result = await repository.GetByInstructorIdAsync(InstructorA);

        Assert.Single(result);
        Assert.Equal(InstructorA, result[0].InstructorId);
    }

    [Fact]
    public async Task GetByIdWithDetailsAsync_ReturnsLessonWithSessionsAndEnrollments()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);
        var course = new Course
        {
            Name = "Networks",
            Department = "CS",
            Code = "CSE320",
            Credits = 3,
            TimesPerWeek = 2
        };
        context.Courses.Add(course);
        await context.SaveChangesAsync();

        var lesson = new Lesson
        {
            InstructorId = InstructorA,
            CourseId = course.Id,
            AcademicYear = 2026,
            Semester = AcademicSemester.Spring,
            CRN = "20001",
            RoomId = 11,
            MaxCapacity = 35
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
        context.LessonEnrollments.Add(new LessonEnrollment { LessonId = lesson.Id, StudentId = StudentA });
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
        var course = new Course
        {
            Name = "AI",
            Department = "CS",
            Code = "CSE400",
            Credits = 3,
            TimesPerWeek = 2
        };
        context.Courses.Add(course);
        await context.SaveChangesAsync();

        context.Lessons.Add(new Lesson
        {
            InstructorId = InstructorA,
            CourseId = course.Id,
            AcademicYear = 2026,
            Semester = AcademicSemester.Spring,
            CRN = "20001",
            RoomId = 1,
            MaxCapacity = 20
        });
        await context.SaveChangesAsync();

        var lessonId = context.Lessons.Single().Id;
        var repository = new LessonRepository(context);
        var exists = await repository.ExistsAsync(MissingStudent, lessonId);

        Assert.False(exists);
    }

    [Fact]
    public async Task GetLessonSchedulingRowsAsync_ReturnsEnrollmentCountAndOrderedByLessonId()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);
        var courseA = new Course
        {
            Name = "Intro CS",
            Department = "CS",
            Code = "CS101",
            Credits = 3,
            TimesPerWeek = 3
        };
        var courseB = new Course
        {
            Name = "Intro CS Lab",
            Department = "CS",
            Code = "CS101L",
            Credits = 1,
            TimesPerWeek = 1
        };
        context.Courses.AddRange(courseA, courseB);
        await context.SaveChangesAsync();

        var lessonA = new Lesson
        {
            InstructorId = InstructorA,
            CourseId = courseA.Id,
            AcademicYear = 2026,
            Semester = AcademicSemester.Fall,
            CRN = "10001",
            RoomId = 1,
            MaxCapacity = 100
        };
        var lessonB = new Lesson
        {
            InstructorId = InstructorB,
            CourseId = courseB.Id,
            AcademicYear = 2026,
            Semester = AcademicSemester.Fall,
            CRN = "10002",
            RoomId = 2,
            MaxCapacity = 24
        };
        context.Lessons.AddRange(lessonA, lessonB);
        await context.SaveChangesAsync();

        context.LessonEnrollments.AddRange(
            new LessonEnrollment { LessonId = lessonA.Id, StudentId = StudentA },
            new LessonEnrollment { LessonId = lessonA.Id, StudentId = StudentB },
            new LessonEnrollment { LessonId = lessonA.Id, StudentId = StudentC });
        await context.SaveChangesAsync();

        var repository = new LessonRepository(context);
        var rows = (await repository.GetLessonSchedulingRowsAsync()).ToList();

        Assert.Equal(2, rows.Count);
        Assert.True(rows[0].LessonId < rows[1].LessonId);
        var rowA = rows.Single(r => r.LessonId == lessonA.Id);
        Assert.Equal(InstructorA, rowA.InstructorUserId);
        Assert.Equal(3, rowA.Enrollment);
        Assert.Equal(3, rowA.TimesPerWeek);
        Assert.Equal("CS101", rowA.CourseCode);
        Assert.Equal("Intro CS", rowA.CourseTitle);
        Assert.Equal(100, rowA.MaxCapacity);
        var rowB = rows.Single(r => r.LessonId == lessonB.Id);
        Assert.Equal(0, rowB.Enrollment);
        Assert.Equal(24, rowB.MaxCapacity);
    }
}
