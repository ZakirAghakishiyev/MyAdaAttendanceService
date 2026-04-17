using Moq;
using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Application.Services;
using MyAdaAttendanceService.Application.Services.Interfaces;
using MyAdaAttendanceService.Core;
using MyAdaAttendanceService.Core.Entities;
using MyAdaAttendanceService.Core.Interfaces;

namespace MyAdaAttendanceService.Application.Tests;

public class LessonServiceTests
{
    private static readonly Guid InstructorA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid InstructorB = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static ICourseRepository DummyCourseRepository()
    {
        var m = new Mock<ICourseRepository>();
        return m.Object;
    }

    private static IExternalUserDirectoryService DummyUserDirectoryService()
    {
        var m = new Mock<IExternalUserDirectoryService>();
        m.Setup(x => x.GetUserByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => new ExternalUserDirectoryDto
            {
                UserId = id,
                DisplayName = "User",
                Email = "user@example.com"
            });
        m.Setup(x => x.GetUsersByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<Guid> ids, CancellationToken _) => (IReadOnlyDictionary<Guid, ExternalUserDirectoryDto>)ids
                .Distinct()
                .ToDictionary(
                    id => id,
                    id => new ExternalUserDirectoryDto
                    {
                        UserId = id,
                        DisplayName = "User",
                        Email = "user@example.com"
                    }));
        m.Setup(x => x.EnsureUserExistsInRoleAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return m.Object;
    }

    [Fact]
    public async Task GetMyLessonByIdAsync_ThrowsUnauthorized_WhenInstructorDoesNotOwnLesson()
    {
        var lesson = new Lesson
        {
            Id = 9,
            InstructorId = InstructorA,
            AcademicYear = 2026,
            Semester = AcademicSemester.Spring,
            CRN = "20001",
            Course = new Course { Code = "CSE310", Department = "CS", Name = "DB", Credits = 3, TimesPerWeek = 2 }
        };

        var lessonRepository = new Mock<ILessonRepository>();
        lessonRepository.Setup(x => x.GetByIdWithDetailsAsync(9)).ReturnsAsync(lesson);
        var service = new LessonService(lessonRepository.Object, DummyCourseRepository(), DummyUserDirectoryService());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetMyLessonByIdAsync(InstructorB, 9));
    }

    [Fact]
    public async Task GetLessonByIdAsync_MapsAndSortsSessions()
    {
        var lesson = new Lesson
        {
            Id = 102,
            InstructorId = InstructorA,
            AcademicYear = 2026,
            Semester = AcademicSemester.Spring,
            CRN = "20001",
            Course = new Course { Code = "CSE201", Department = "CS", Name = "Algorithms", Credits = 3, TimesPerWeek = 2 },
            Sessions = new List<LessonSession>
            {
                new() { Id = 2, Date = new DateOnly(2026, 4, 12), StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(11, 0), Topic = "B" },
                new() { Id = 1, Date = new DateOnly(2026, 4, 11), StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(10, 0), Topic = "A" }
            }
        };

        var lessonRepository = new Mock<ILessonRepository>();
        lessonRepository
            .Setup(x => x.GetByIdWithDetailsAsync(102))
            .ReturnsAsync(lesson);

        var service = new LessonService(lessonRepository.Object, DummyCourseRepository(), DummyUserDirectoryService());

        var result = await service.GetLessonByIdAsync(102);

        Assert.Equal(102, result.Id);
        Assert.NotNull(result.Sessions);
        Assert.Equal(new[] { 1, 2 }, result.Sessions!.Select(x => x.Id).ToArray());
    }

    [Fact]
    public async Task GetAllLessonsAsync_ReturnsLessonsOrderedById()
    {
        var lessonRepository = new Mock<ILessonRepository>();
        lessonRepository.Setup(x => x.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Lesson, bool>>?>(),
                It.IsAny<Func<IQueryable<Lesson>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Lesson, object>>?>(),
                It.IsAny<Func<IQueryable<Lesson>, IOrderedQueryable<Lesson>>?>(),
                It.IsAny<bool>()))
            .ReturnsAsync(new List<Lesson>
            {
                new()
                {
                    Id = 1,
                    InstructorId = InstructorA,
                    AcademicYear = 2026,
                    Semester = AcademicSemester.Spring,
                    CRN = "20001",
                    Course = new Course { Code = "A1", Department = "D", Name = "A", Credits = 3, TimesPerWeek = 1 }
                },
                new()
                {
                    Id = 2,
                    InstructorId = InstructorA,
                    AcademicYear = 2026,
                    Semester = AcademicSemester.Spring,
                    CRN = "20002",
                    Course = new Course { Code = "B1", Department = "D", Name = "B", Credits = 3, TimesPerWeek = 1 }
                }
            });

        var service = new LessonService(lessonRepository.Object, DummyCourseRepository(), DummyUserDirectoryService());
        var result = (await service.GetAllLessonsAsync()).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(new[] { 1, 2 }, result.Select(x => x.Id).ToArray());
    }

    [Fact]
    public async Task CreateLessonAsync_ThrowsWhenInstructorIdIsInvalid()
    {
        var lessonRepository = new Mock<ILessonRepository>();
        var courseRepository = new Mock<ICourseRepository>();
        var service = new LessonService(lessonRepository.Object, courseRepository.Object, DummyUserDirectoryService());
        var dto = new CreateLessonDto
        {
            CourseId = 1,
            InstructorId = Guid.Empty
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateLessonAsync(dto));
    }

    [Fact]
    public async Task CreateLessonAsync_TrimsStringFieldsAndUsesGeneratedCrn()
    {
        Lesson? passedToAdd = null;
        string? crnWhenRepositoryReceived = null;
        var lessonRepository = new Mock<ILessonRepository>();
        lessonRepository.Setup(x => x.AddWithGeneratedCrnAsync(It.IsAny<Lesson>()))
            .Callback<Lesson>(l =>
            {
                passedToAdd = l;
                crnWhenRepositoryReceived = l.CRN;
            })
            .ReturnsAsync((Lesson l) =>
            {
                l.Id = 99;
                l.CRN = "20001";
                return l;
            });

        var courseRepository = new Mock<ICourseRepository>();
        courseRepository.Setup(x => x.GetByIdAsync(42)).ReturnsAsync(new Course
        {
            Id = 42,
            Name = "Algorithms",
            Department = "CS",
            Code = "CSE201",
            Credits = 3,
            TimesPerWeek = 2
        });

        var service = new LessonService(lessonRepository.Object, courseRepository.Object, DummyUserDirectoryService());
        var dto = new CreateLessonDto
        {
            CourseId = 42,
            InstructorId = InstructorA,
            RoomId = 3,
            AcademicYear = 2026,
            Semester = AcademicSemester.Spring,
            MaxCapacity = 40
        };

        var created = await service.CreateLessonAsync(dto);

        courseRepository.Verify(x => x.GetByIdAsync(42), Times.Once);
        lessonRepository.Verify(x => x.AddWithGeneratedCrnAsync(It.IsAny<Lesson>()), Times.Once);
        Assert.NotNull(passedToAdd);
        Assert.Equal(AcademicSemester.Spring, passedToAdd!.Semester);
        Assert.Equal(2026, passedToAdd.AcademicYear);
        Assert.Equal(42, passedToAdd.CourseId);
        Assert.Equal(string.Empty, crnWhenRepositoryReceived);
        Assert.Equal("Algorithms", created.Name);
        Assert.Equal("CSE201", created.Code);
        Assert.Equal("20001", created.CRN);
    }

    [Fact]
    public async Task GetLessonsForSchedulingAsync_DelegatesToRepository()
    {
        var rows = new List<LessonSchedulingRow>
        {
            new()
            {
                LessonId = 10,
                InstructorUserId = InstructorB,
                Enrollment = 12,
                MaxCapacity = 40,
                TimesPerWeek = 2,
                CourseCode = "MTH101",
                CourseTitle = "Calculus",
            }
        };
        var lessonRepository = new Mock<ILessonRepository>();
        lessonRepository.Setup(x => x.GetLessonSchedulingRowsAsync()).ReturnsAsync(rows);

        var service = new LessonService(lessonRepository.Object, DummyCourseRepository(), DummyUserDirectoryService());
        var result = await service.GetLessonsForSchedulingAsync();

        Assert.Same(rows, result);
    }
}
