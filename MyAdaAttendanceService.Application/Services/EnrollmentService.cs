using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Application.Services.Interfaces;
using MyAdaAttendanceService.Core.Interfaces;

namespace MyAdaAttendanceService.Application.Services;

public class EnrollmentService : IEnrollmentService
{
    private readonly ILessonEnrollmentRepository _enrollmentRepository;
    private readonly ILessonRepository _lessonRepository;
    private readonly IExternalUserDirectoryService _userDirectoryService;

    public EnrollmentService(
        ILessonEnrollmentRepository enrollmentRepository,
        ILessonRepository lessonRepository,
        IExternalUserDirectoryService userDirectoryService)
    {
        _enrollmentRepository = enrollmentRepository;
        _lessonRepository = lessonRepository;
        _userDirectoryService = userDirectoryService;
    }

    public async Task<bool> IsStudentEnrolledAsync(Guid studentId, int lessonId)
    {
        return await _enrollmentRepository.ExistsAsync(lessonId, studentId);
    }

    public async Task<IEnumerable<StudentDto>> GetStudentsByLessonAsync(int lessonId)
    {
        var enrollments = await _enrollmentRepository.GetByLessonIdAsync(lessonId);
        var users = await _userDirectoryService.GetUsersByIdsAsync(enrollments.Select(x => x.StudentId));

        return enrollments.Select(e => new StudentDto
        {
            Id = e.StudentId,
            FullName = users.TryGetValue(e.StudentId, out var user) ? user.DisplayName : string.Empty,
            StudentCode = users.TryGetValue(e.StudentId, out user) ? user.UserName : string.Empty,
            Email = users.TryGetValue(e.StudentId, out user) ? user.Email : null
        });
    }

    public async Task<IEnumerable<EnrollmentDto>> GetEnrollmentsByLessonAsync(int lessonId)
    {
        var enrollments = await _enrollmentRepository.GetByLessonIdAsync(lessonId);
        var users = await _userDirectoryService.GetUsersByIdsAsync(enrollments.Select(x => x.StudentId));

        return enrollments.Select(e => new EnrollmentDto
        {
            Id = e.Id,
            LessonId = e.LessonId,
            StudentId = e.StudentId,
            StudentFullName = users.TryGetValue(e.StudentId, out var user) ? user.DisplayName : string.Empty,
            StudentCode = users.TryGetValue(e.StudentId, out user) ? user.UserName : string.Empty,
            StudentEmail = users.TryGetValue(e.StudentId, out user) ? user.Email : string.Empty
        });
    }

    public async Task<EnrollmentDto> CreateEnrollmentAsync(int lessonId, CreateEnrollmentDto dto)
    {
        if (dto.StudentId == Guid.Empty)
            throw new ArgumentException("StudentId is required.");

        _ = await _lessonRepository.GetByIdAsync(lessonId);
        await _userDirectoryService.EnsureUserExistsInRoleAsync(dto.StudentId, "student");

        if (await _enrollmentRepository.ExistsAsync(lessonId, dto.StudentId))
            throw new InvalidOperationException("Student is already enrolled in this lesson.");

        var enrollment = await _enrollmentRepository.AddAsync(new Core.Entities.LessonEnrollment
        {
            LessonId = lessonId,
            StudentId = dto.StudentId
        });

        var user = await _userDirectoryService.GetUserByIdAsync(dto.StudentId);

        return new EnrollmentDto
        {
            Id = enrollment.Id,
            LessonId = enrollment.LessonId,
            StudentId = enrollment.StudentId,
            StudentFullName = user?.DisplayName ?? string.Empty,
            StudentCode = user?.UserName ?? string.Empty,
            StudentEmail = user?.Email ?? string.Empty
        };
    }
}
