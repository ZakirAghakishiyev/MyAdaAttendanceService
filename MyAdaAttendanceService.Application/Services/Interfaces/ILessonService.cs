using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Core;

namespace MyAdaAttendanceService.Application.Services.Interfaces;

public interface ILessonService
{
    Task<IEnumerable<LessonDto>> GetMyLessonsAsync(Guid instructorId);

    Task<LessonDto> GetMyLessonByIdAsync(Guid instructorId, int lessonId);

    Task<IEnumerable<LessonDto>> GetAllLessonsAsync();

    Task<LessonDto> GetLessonByIdAsync(int lessonId);

    Task<IEnumerable<LessonDto>> GetLessonsByCourseIdAsync(int courseId);

    Task<IEnumerable<LessonDto>> GetLessonsByAcademicTermAsync(int academicYear, AcademicSemester semester);

    /// <summary>Creates a lesson for an existing course; <see cref="CreateLessonDto.CourseId"/> and <see cref="CreateLessonDto.InstructorId"/> are required.</summary>
    Task<LessonDto> CreateLessonAsync(CreateLessonDto dto);

    Task<LessonDto> UpdateLessonAsync(int lessonId, UpdateLessonDto dto);

    Task DeleteLessonAsync(int lessonId);

    /// <summary>Lessons with scheduling fields for downstream services (enrollment count, instructor user id, display metadata).</summary>
    Task<IReadOnlyList<LessonSchedulingRow>> GetLessonsForSchedulingAsync();
}