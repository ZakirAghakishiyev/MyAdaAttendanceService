using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Application.Services.Interfaces;
using MyAdaAttendanceService.Core.Entities;
using MyAdaAttendanceService.Core.Interfaces;

namespace MyAdaAttendanceService.Application.Services;

public class LessonService : ILessonService
{
    private readonly ILessonRepository _lessonRepository;

    public LessonService(ILessonRepository lessonRepository)
    {
        _lessonRepository = lessonRepository;
    }

    public async Task<IEnumerable<LessonDto>> GetMyLessonsAsync(int instructorId)
    {
        var lessons = await _lessonRepository.GetByInstructorIdAsync(instructorId);

        return lessons.Select(l => new LessonDto
        {
            Id = l.Id,
            Name = l.Name,
            Code = l.Code,
            InstructorId = l.InstructorId
        });
    }

    public async Task<LessonDto> GetMyLessonByIdAsync(int instructorId, int lessonId)
    {
        var lesson = await _lessonRepository.GetByIdWithDetailsAsync(lessonId)
            ?? throw new KeyNotFoundException($"Lesson {lessonId} not found.");

        if (lesson.InstructorId != instructorId)
            throw new UnauthorizedAccessException("You do not own this lesson.");

        return new LessonDto
        {
            Id = lesson.Id,
            Name = lesson.Name,
            Code = lesson.Code,
            InstructorId = lesson.InstructorId,
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
        var lessons = await _lessonRepository.GetAllAsync(orderBy: q => q.OrderBy(l => l.Id));

        return lessons.Select(l => new LessonDto
        {
            Id = l.Id,
            Name = l.Name,
            Code = l.Code,
            InstructorId = l.InstructorId
        });
    }

    public async Task<LessonDto> GetLessonByIdAsync(int lessonId)
    {
        var lesson = await _lessonRepository.GetByIdWithDetailsAsync(lessonId)
            ?? throw new KeyNotFoundException($"Lesson {lessonId} not found.");

        return new LessonDto
        {
            Id = lesson.Id,
            Name = lesson.Name,
            Code = lesson.Code,
            InstructorId = lesson.InstructorId,
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

    public async Task<LessonDto> CreateLessonAsync(CreateLessonDto dto)
    {
        if (dto.InstructorId <= 0)
            throw new ArgumentException("InstructorId is required and must be positive.");
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Lesson name is required.");
        if (string.IsNullOrWhiteSpace(dto.Code))
            throw new ArgumentException("Lesson code is required.");
        if (dto.Credits < 0)
            throw new ArgumentException("Credits cannot be negative.");
        if (dto.TimesPerWeek < 0)
            throw new ArgumentException("Times per week cannot be negative.");
        if (dto.Capacity < 0)
            throw new ArgumentException("Capacity cannot be negative.");

        var lesson = new Lesson
        {
            InstructorId = dto.InstructorId,
            RoomId = dto.RoomId,
            Semester = dto.Semester.Trim(),
            CRN = dto.CRN.Trim(),
            Name = dto.Name.Trim(),
            Type = dto.Type.Trim(),
            Department = dto.Department.Trim(),
            Code = dto.Code.Trim(),
            Credits = dto.Credits,
            TimesPerWeek = dto.TimesPerWeek,
            Capacity = dto.Capacity
        };

        await _lessonRepository.AddAsync(lesson);

        return new LessonDto
        {
            Id = lesson.Id,
            Name = lesson.Name,
            Code = lesson.Code,
            InstructorId = lesson.InstructorId
        };
    }
}
