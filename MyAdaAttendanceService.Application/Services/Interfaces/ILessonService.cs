using MyAdaAttendanceService.Application.DTOs;

namespace MyAdaAttendanceService.Application.Services.Interfaces;

public interface ILessonService
{
    Task<IEnumerable<LessonDto>> GetMyLessonsAsync(int instructorId);

    Task<LessonDto> GetMyLessonByIdAsync(int instructorId, int lessonId);

    Task<IEnumerable<LessonDto>> GetAllLessonsAsync();

    Task<LessonDto> GetLessonByIdAsync(int lessonId);

    /// <summary>Creates a lesson; <see cref="CreateLessonDto.InstructorId"/> must identify the assigned instructor (e.g. office workflow).</summary>
    Task<LessonDto> CreateLessonAsync(CreateLessonDto dto);
}