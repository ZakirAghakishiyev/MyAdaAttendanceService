using Moq;
using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Application.Services;
using MyAdaAttendanceService.Application.Services.Interfaces;
using MyAdaAttendanceService.Core.Entities;
using MyAdaAttendanceService.Core.Interfaces;

namespace MyAdaAttendanceService.Application.Tests;

public class EnrollmentServiceTests
{
    private static readonly Guid StudentA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid StudentB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private static EnrollmentService CreateService(Mock<ILessonEnrollmentRepository> repo)
    {
        var lessonRepo = new Mock<ILessonRepository>();
        lessonRepo.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new Lesson { Id = 10, CRN = "20001" });

        var userDirectory = new Mock<IExternalUserDirectoryService>();
        userDirectory.Setup(x => x.GetUsersByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<Guid> ids, CancellationToken _) => (IReadOnlyDictionary<Guid, ExternalUserDirectoryDto>)ids
                .Distinct()
                .ToDictionary(
                    id => id,
                    id => new ExternalUserDirectoryDto
                    {
                        UserId = id,
                        DisplayName = $"Student-{id.ToString()[..8]}",
                        UserName = $"student-{id.ToString()[..8]}",
                        Email = $"student-{id.ToString()[..8]}@example.com"
                    }));
        userDirectory.Setup(x => x.GetUserByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => new ExternalUserDirectoryDto
            {
                UserId = id,
                DisplayName = $"Student-{id.ToString()[..8]}",
                UserName = $"student-{id.ToString()[..8]}",
                Email = $"student-{id.ToString()[..8]}@example.com",
                Role = "student"
            });
        userDirectory.Setup(x => x.EnsureUserExistsInRoleAsync(It.IsAny<Guid>(), "student", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return new EnrollmentService(repo.Object, lessonRepo.Object, userDirectory.Object);
    }

    [Fact]
    public async Task IsStudentEnrolledAsync_ReturnsRepositoryResult()
    {
        var repo = new Mock<ILessonEnrollmentRepository>();
        repo.Setup(x => x.ExistsAsync(10, StudentA)).ReturnsAsync(true);
        var service = CreateService(repo);

        var result = await service.IsStudentEnrolledAsync(StudentA, 10);

        Assert.True(result);
    }

    [Fact]
    public async Task GetStudentsByLessonAsync_MapsStudentIds()
    {
        var repo = new Mock<ILessonEnrollmentRepository>();
        repo.Setup(x => x.GetByLessonIdAsync(10)).ReturnsAsync(new List<LessonEnrollment>
        {
            new() { Id = 1, LessonId = 10, StudentId = StudentA },
            new() { Id = 2, LessonId = 10, StudentId = StudentB }
        });
        var service = CreateService(repo);

        var students = (await service.GetStudentsByLessonAsync(10)).ToList();

        Assert.Equal(new[] { StudentA, StudentB }, students.Select(s => s.Id).ToArray());
    }

    [Fact]
    public async Task GetEnrollmentsByLessonAsync_MapsEnrollmentFields()
    {
        var repo = new Mock<ILessonEnrollmentRepository>();
        repo.Setup(x => x.GetByLessonIdAsync(10)).ReturnsAsync(new List<LessonEnrollment>
        {
            new() { Id = 9, LessonId = 10, StudentId = StudentA }
        });
        var service = CreateService(repo);

        var enrollments = (await service.GetEnrollmentsByLessonAsync(10)).ToList();

        Assert.Single(enrollments);
        Assert.Equal(9, enrollments[0].Id);
        Assert.Equal(10, enrollments[0].LessonId);
        Assert.Equal(StudentA, enrollments[0].StudentId);
    }

    [Fact]
    public async Task GetStudentsByLessonAsync_ReturnsEmpty_WhenNoEnrollments()
    {
        var repo = new Mock<ILessonEnrollmentRepository>();
        repo.Setup(x => x.GetByLessonIdAsync(10)).ReturnsAsync(new List<LessonEnrollment>());
        var service = CreateService(repo);

        var students = await service.GetStudentsByLessonAsync(10);

        Assert.Empty(students);
    }
}

