using MyAdaAttendanceService.Application.DTOs;

namespace MyAdaAttendanceService.Application.Services.Interfaces;

public interface ICourseService
{
    Task<CourseDto> CreateAsync(CreateCourseDto dto);

    Task<IEnumerable<CourseDto>> GetAllAsync();

    Task<CourseDto> GetByIdAsync(int id);

    Task<CourseDto> UpdateAsync(int id, UpdateCourseDto dto);

    Task DeleteAsync(int id);
}
