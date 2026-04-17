using Microsoft.EntityFrameworkCore;
using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Application.Services.Interfaces;
using MyAdaAttendanceService.Core;
using MyAdaAttendanceService.Core.Entities;
using MyAdaAttendanceService.Core.Interfaces;

namespace MyAdaAttendanceService.Application.Services;

public class LessonService : ILessonService
{
    private readonly ILessonRepository _lessonRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly IExternalUserDirectoryService _userDirectoryService;

    public LessonService(
        ILessonRepository lessonRepository,
        ICourseRepository courseRepository,
        IExternalUserDirectoryService userDirectoryService)
    {
        _lessonRepository = lessonRepository;
        _courseRepository = courseRepository;
        _userDirectoryService = userDirectoryService;
    }

    private static void ValidateAcademicYear(int academicYear)
    {
        if (academicYear is < 2000 or > 2100)
            throw new ArgumentException("Academic year must be between 2000 and 2100.");
    }

    private static void ValidateSemester(AcademicSemester semester)
    {
        if (!Enum.IsDefined(semester))
            throw new ArgumentException("Semester must be Fall, Spring, or Summer.");
    }

    private static LessonDto MapLessonSummary(Lesson l, ExternalUserDirectoryDto? instructor = null) => new()
    {
        Id = l.Id,
        Name = l.Course!.Name,
        Code = l.Course.Code,
        InstructorId = l.InstructorId,
        InstructorDisplayName = instructor?.DisplayName ?? string.Empty,
        InstructorEmail = instructor?.Email ?? string.Empty,
        AcademicYear = l.AcademicYear,
        Semester = l.Semester,
        CRN = l.CRN,
        MaxCapacity = l.MaxCapacity
    };

    public async Task<IEnumerable<LessonDto>> GetMyLessonsAsync(Guid instructorId)
    {
        var lessons = await _lessonRepository.GetByInstructorIdAsync(instructorId);
        var users = await _userDirectoryService.GetUsersByIdsAsync(lessons.Select(x => x.InstructorId));
        return lessons.Select(x => MapLessonSummary(x, users.GetValueOrDefault(x.InstructorId)));
    }

    public async Task<LessonDto> GetMyLessonByIdAsync(Guid instructorId, int lessonId)
    {
        var lesson = await _lessonRepository.GetByIdWithDetailsAsync(lessonId)
            ?? throw new KeyNotFoundException($"Lesson {lessonId} not found.");

        if (lesson.InstructorId != instructorId)
            throw new UnauthorizedAccessException("You do not own this lesson.");

        var instructor = await _userDirectoryService.GetUserByIdAsync(lesson.InstructorId);
        return new LessonDto
        {
            Id = lesson.Id,
            Name = lesson.Course!.Name,
            Code = lesson.Course.Code,
            InstructorId = lesson.InstructorId,
            InstructorDisplayName = instructor?.DisplayName ?? string.Empty,
            InstructorEmail = instructor?.Email ?? string.Empty,
            AcademicYear = lesson.AcademicYear,
            Semester = lesson.Semester,
            CRN = lesson.CRN,
            MaxCapacity = lesson.MaxCapacity,
            Sessions = lesson.Sessions
                .OrderBy(s => s.Date).ThenBy(s => s.StartTime)
                .Select(s => new SessionShortDto
                {
                    Id = s.Id,
                    Date = s.Date,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    Topic = s.Topic,
                    IsAttendanceActive = s.IsAttendanceActive
                }).ToList()
        };
    }

    public async Task<IEnumerable<LessonDto>> GetAllLessonsAsync()
    {
        var lessons = await _lessonRepository.GetAllAsync(
            include: q => q.Include(l => l.Course),
            orderBy: q => q.OrderBy(l => l.Id));

        var users = await _userDirectoryService.GetUsersByIdsAsync(lessons.Select(x => x.InstructorId));
        return lessons.Select(x => MapLessonSummary(x, users.GetValueOrDefault(x.InstructorId)));
    }

    public async Task<LessonDto> GetLessonByIdAsync(int lessonId)
    {
        var lesson = await _lessonRepository.GetByIdWithDetailsAsync(lessonId)
            ?? throw new KeyNotFoundException($"Lesson {lessonId} not found.");

        var instructor = await _userDirectoryService.GetUserByIdAsync(lesson.InstructorId);
        return new LessonDto
        {
            Id = lesson.Id,
            Name = lesson.Course!.Name,
            Code = lesson.Course.Code,
            InstructorId = lesson.InstructorId,
            InstructorDisplayName = instructor?.DisplayName ?? string.Empty,
            InstructorEmail = instructor?.Email ?? string.Empty,
            AcademicYear = lesson.AcademicYear,
            Semester = lesson.Semester,
            CRN = lesson.CRN,
            MaxCapacity = lesson.MaxCapacity,
            Sessions = lesson.Sessions
                .OrderBy(s => s.Date).ThenBy(s => s.StartTime)
                .Select(s => new SessionShortDto
                {
                    Id = s.Id,
                    Date = s.Date,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    Topic = s.Topic,
                    IsAttendanceActive = s.IsAttendanceActive
                }).ToList()
        };
    }

    public async Task<IEnumerable<LessonDto>> GetLessonsByCourseIdAsync(int courseId)
    {
        _ = await _courseRepository.GetByIdAsync(courseId);

        var lessons = await _lessonRepository.GetByCourseIdAsync(courseId);

        var users = await _userDirectoryService.GetUsersByIdsAsync(lessons.Select(x => x.InstructorId));
        return lessons.Select(x => MapLessonSummary(x, users.GetValueOrDefault(x.InstructorId)));
    }

    public async Task<IEnumerable<LessonDto>> GetLessonsByAcademicTermAsync(int academicYear, AcademicSemester semester)
    {
        ValidateAcademicYear(academicYear);
        ValidateSemester(semester);

        var lessons = await _lessonRepository.GetByAcademicTermAsync(academicYear, semester);
        var users = await _userDirectoryService.GetUsersByIdsAsync(lessons.Select(x => x.InstructorId));
        return lessons.Select(x => MapLessonSummary(x, users.GetValueOrDefault(x.InstructorId)));
    }

    public async Task<LessonDto> CreateLessonAsync(CreateLessonDto dto)
    {
        if (dto.CourseId <= 0)
            throw new ArgumentException("CourseId is required and must be positive.");
        if (dto.InstructorId == Guid.Empty)
            throw new ArgumentException("InstructorId is required.");
        if (dto.MaxCapacity < 0)
            throw new ArgumentException("Max capacity cannot be negative.");

        ValidateAcademicYear(dto.AcademicYear);
        ValidateSemester(dto.Semester);
        await _userDirectoryService.EnsureUserExistsInRoleAsync(dto.InstructorId, "instructor");

        var course = await _courseRepository.GetByIdAsync(dto.CourseId);

        var lesson = new Lesson
        {
            InstructorId = dto.InstructorId,
            RoomId = dto.RoomId,
            CourseId = course.Id,
            AcademicYear = dto.AcademicYear,
            Semester = dto.Semester,
            CRN = string.Empty,
            MaxCapacity = dto.MaxCapacity
        };

        await _lessonRepository.AddWithGeneratedCrnAsync(lesson);

        var instructor = await _userDirectoryService.GetUserByIdAsync(lesson.InstructorId);
        return new LessonDto
        {
            Id = lesson.Id,
            Name = course.Name,
            Code = course.Code,
            InstructorId = lesson.InstructorId,
            InstructorDisplayName = instructor?.DisplayName ?? string.Empty,
            InstructorEmail = instructor?.Email ?? string.Empty,
            AcademicYear = lesson.AcademicYear,
            Semester = lesson.Semester,
            CRN = lesson.CRN,
            MaxCapacity = lesson.MaxCapacity
        };
    }

    public async Task<LessonDto> UpdateLessonAsync(int lessonId, UpdateLessonDto dto)
    {
        if (dto.CourseId <= 0)
            throw new ArgumentException("CourseId is required and must be positive.");
        if (dto.InstructorId == Guid.Empty)
            throw new ArgumentException("InstructorId is required.");
        if (dto.MaxCapacity < 0)
            throw new ArgumentException("Max capacity cannot be negative.");

        ValidateAcademicYear(dto.AcademicYear);
        ValidateSemester(dto.Semester);
        await _userDirectoryService.EnsureUserExistsInRoleAsync(dto.InstructorId, "instructor");

        var lesson = await _lessonRepository.GetByIdAsync(lessonId);
        _ = await _courseRepository.GetByIdAsync(dto.CourseId);

        var termChanged = lesson.AcademicYear != dto.AcademicYear || lesson.Semester != dto.Semester;

        await _lessonRepository.UpdateLessonWithAutoCrnAsync(
            lesson,
            dto.AcademicYear,
            dto.Semester,
            dto.CourseId,
            termChanged,
            dto.InstructorId,
            dto.RoomId,
            dto.MaxCapacity);

        return await GetLessonByIdAsync(lessonId);
    }

    public async Task DeleteLessonAsync(int lessonId)
    {
        var lesson = await _lessonRepository.GetByIdAsync(lessonId);
        await _lessonRepository.RemoveAsync(lesson);
    }

    public Task<IReadOnlyList<LessonSchedulingRow>> GetLessonsForSchedulingAsync()
    {
        return _lessonRepository.GetLessonSchedulingRowsAsync();
    }
}
