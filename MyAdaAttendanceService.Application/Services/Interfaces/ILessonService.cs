using MyAdaAttendanceService.Application.DTOs;

namespace MyAdaAttendanceService.Application.Services.Interfaces;

public interface ILessonService
{
    Task<IEnumerable<LessonDto>> GetMyLessonsAsync(int instructorId);

    Task<LessonDto> GetMyLessonByIdAsync(int instructorId, int lessonId);
}
