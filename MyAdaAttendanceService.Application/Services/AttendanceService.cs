namespace MyAdaAttendanceService.Application.Services;

using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Application.Services.Interfaces;
using MyAdaAttendanceService.Core.Entities;
using MyAdaAttendanceService.Core.Interfaces;

public class LessonService : ILessonService
{
    private readonly IRepository<Lesson> _lessonRepository;
    private readonly IMapper _mapper;

    public LessonService(IRepository<Lesson> lessonRepository, IMapper mapper)
    {
        _lessonRepository = lessonRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<LessonDto>> GetMyLessonsAsync(int instructorId)
    {
        var lessons = await _lessonRepository.GetAllAsync(
            l => l.InstructorId == instructorId,
            include: q => q.Include(x => x.Sessions),
            asNoTracking: true);

        return lessons.Select(l => new LessonDto
        {
            Id = l.Id,
            Name = l.Name,
            Code = l.Code,
            InstructorId = l.InstructorId,
            Sessions = l.Sessions?
                .OrderBy(s => s.Date)
                .ThenBy(s => s.StartTime)
                .Select(s => new SessionShortDto
                {
                    Id = s.Id,
                    Date = s.Date,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime
                })
                .ToList()
        });
    }

    public async Task<LessonDto> GetMyLessonByIdAsync(int instructorId, int lessonId)
    {
        var lesson = await _lessonRepository.GetAsync(
            l => l.Id == lessonId,
            include: q => q.Include(x => x.Sessions),
            asNoTracking: true);

        if (lesson == null)
            throw new KeyNotFoundException($"Lesson with id {lessonId} was not found.");

        if (lesson.InstructorId != instructorId)
            throw new UnauthorizedAccessException("You are not allowed to access this lesson.");

        return new LessonDto
        {
            Id = lesson.Id,
            Name = lesson.Name,
            Code = lesson.Code,
            InstructorId = lesson.InstructorId,
            Sessions = l.Sessions?
                .OrderBy(s => s.Date)
                .ThenBy(s => s.StartTime)
                .Select(s => new SessionShortDto
                {
                    Id = s.Id,
                    Date = s.Date,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime
                })
                .ToList()
        };
    }
}


