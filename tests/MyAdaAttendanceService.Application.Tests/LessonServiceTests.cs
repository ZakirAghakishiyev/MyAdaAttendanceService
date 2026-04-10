using Moq;
using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Application.Services;
using MyAdaAttendanceService.Core.Entities;
using MyAdaAttendanceService.Core.Interfaces;

namespace MyAdaAttendanceService.Application.Tests;

public class LessonServiceTests
{
    [Fact]
    public async Task GetMyLessonByIdAsync_ThrowsUnauthorized_WhenInstructorDoesNotOwnLesson()
    {
        var lesson = new Lesson
        {
            Id = 9,
            InstructorId = 77,
            Name = "Databases",
            Code = "CSE310"
        };

        var lessonRepository = new Mock<ILessonRepository>();
        lessonRepository.Setup(x => x.GetByIdWithDetailsAsync(9)).ReturnsAsync(lesson);
        var service = new LessonService(lessonRepository.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetMyLessonByIdAsync(10, 9));
    }

    [Fact]
    public async Task GetLessonByIdAsync_MapsAndSortsSessions()
    {
        var lesson = new Lesson
        {
            Id = 102,
            Name = "Algorithms",
            Code = "CSE201",
            InstructorId = 10,
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

        var service = new LessonService(lessonRepository.Object);

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
                new() { Id = 1, Name = "A", Code = "A1", InstructorId = 1 },
                new() { Id = 2, Name = "B", Code = "B1", InstructorId = 1 }
            });

        var service = new LessonService(lessonRepository.Object);
        var result = (await service.GetAllLessonsAsync()).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(new[] { 1, 2 }, result.Select(x => x.Id).ToArray());
    }

    [Fact]
    public async Task CreateLessonAsync_ThrowsWhenInstructorIdIsInvalid()
    {
        var lessonRepository = new Mock<ILessonRepository>();
        var service = new LessonService(lessonRepository.Object);
        var dto = new CreateLessonDto
        {
            InstructorId = 0,
            Name = "Algorithms",
            Code = "CSE201"
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateLessonAsync(dto));
    }

    [Fact]
    public async Task CreateLessonAsync_TrimsStringFieldsBeforePersisting()
    {
        var lessonRepository = new Mock<ILessonRepository>();
        lessonRepository.Setup(x => x.AddAsync(It.IsAny<Lesson>()))
            .ReturnsAsync((Lesson l) => l);

        var service = new LessonService(lessonRepository.Object);
        var dto = new CreateLessonDto
        {
            InstructorId = 12,
            RoomId = 3,
            Semester = "  Spring  ",
            CRN = "  1001  ",
            Name = "  Algorithms  ",
            Type = "  Core  ",
            Department = "  CS  ",
            Code = "  CSE201  ",
            Credits = 3,
            TimesPerWeek = 2,
            Capacity = 40
        };

        var created = await service.CreateLessonAsync(dto);

        lessonRepository.Verify(x => x.AddAsync(It.Is<Lesson>(l =>
            l.Semester == "Spring" &&
            l.CRN == "1001" &&
            l.Name == "Algorithms" &&
            l.Type == "Core" &&
            l.Department == "CS" &&
            l.Code == "CSE201")), Times.Once);
        Assert.Equal("Algorithms", created.Name);
        Assert.Equal("CSE201", created.Code);
    }
}
