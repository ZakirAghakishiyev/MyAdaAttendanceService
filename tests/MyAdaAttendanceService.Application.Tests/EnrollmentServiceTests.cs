using Moq;
using MyAdaAttendanceService.Application.Services;
using MyAdaAttendanceService.Core.Entities;
using MyAdaAttendanceService.Core.Interfaces;

namespace MyAdaAttendanceService.Application.Tests;

public class EnrollmentServiceTests
{
    [Fact]
    public async Task IsStudentEnrolledAsync_ReturnsRepositoryResult()
    {
        var repo = new Mock<ILessonEnrollmentRepository>();
        repo.Setup(x => x.ExistsAsync(10, 5)).ReturnsAsync(true);
        var service = new EnrollmentService(repo.Object);

        var result = await service.IsStudentEnrolledAsync(5, 10);

        Assert.True(result);
    }

    [Fact]
    public async Task GetStudentsByLessonAsync_MapsStudentIds()
    {
        var repo = new Mock<ILessonEnrollmentRepository>();
        repo.Setup(x => x.GetByLessonIdAsync(10)).ReturnsAsync(new List<LessonEnrollment>
        {
            new() { Id = 1, LessonId = 10, StudentId = 7 },
            new() { Id = 2, LessonId = 10, StudentId = 8 }
        });
        var service = new EnrollmentService(repo.Object);

        var students = (await service.GetStudentsByLessonAsync(10)).ToList();

        Assert.Equal(new[] { 7, 8 }, students.Select(s => s.Id).ToArray());
    }

    [Fact]
    public async Task GetEnrollmentsByLessonAsync_MapsEnrollmentFields()
    {
        var repo = new Mock<ILessonEnrollmentRepository>();
        repo.Setup(x => x.GetByLessonIdAsync(10)).ReturnsAsync(new List<LessonEnrollment>
        {
            new() { Id = 9, LessonId = 10, StudentId = 7 }
        });
        var service = new EnrollmentService(repo.Object);

        var enrollments = (await service.GetEnrollmentsByLessonAsync(10)).ToList();

        Assert.Single(enrollments);
        Assert.Equal(9, enrollments[0].Id);
        Assert.Equal(10, enrollments[0].LessonId);
        Assert.Equal(7, enrollments[0].StudentId);
    }

    [Fact]
    public async Task GetStudentsByLessonAsync_ReturnsEmpty_WhenNoEnrollments()
    {
        var repo = new Mock<ILessonEnrollmentRepository>();
        repo.Setup(x => x.GetByLessonIdAsync(10)).ReturnsAsync(new List<LessonEnrollment>());
        var service = new EnrollmentService(repo.Object);

        var students = await service.GetStudentsByLessonAsync(10);

        Assert.Empty(students);
    }
}

