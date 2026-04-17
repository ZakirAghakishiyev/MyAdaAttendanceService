using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Application.Services.Interfaces;
using MyAdaAttendanceService.Core.Entities;
using MyAdaAttendanceService.Core.Interfaces;

namespace MyAdaAttendanceService.Application.Services;

public class CourseService : ICourseService
{
    private readonly ICourseRepository _courseRepository;

    public CourseService(ICourseRepository courseRepository)
    {
        _courseRepository = courseRepository;
    }

    public async Task<CourseDto> CreateAsync(CreateCourseDto dto)
    {
        ValidateCommon(dto.Name, dto.Department, dto.Code, dto.Credits, dto.TimesPerWeek);

        var dept = dto.Department.Trim();
        var code = dto.Code.Trim();
        var existing = await _courseRepository.FindByDepartmentAndCodeAsync(dept, code);
        if (existing != null)
            throw new InvalidOperationException($"A course with department '{dept}' and code '{code}' already exists.");

        var course = new Course
        {
            Name = dto.Name.Trim(),
            Department = dept,
            Code = code,
            Credits = dto.Credits,
            TimesPerWeek = dto.TimesPerWeek
        };

        await _courseRepository.AddAsync(course);
        return Map(course);
    }

    public async Task<IEnumerable<CourseDto>> GetAllAsync()
    {
        var list = await _courseRepository.GetAllAsync(orderBy: q => q.OrderBy(c => c.Department).ThenBy(c => c.Code));
        return list.Select(Map);
    }

    public async Task<CourseDto> GetByIdAsync(int id)
    {
        var course = await _courseRepository.GetByIdAsync(id);
        return Map(course);
    }

    public async Task<CourseDto> UpdateAsync(int id, UpdateCourseDto dto)
    {
        ValidateCommon(dto.Name, dto.Department, dto.Code, dto.Credits, dto.TimesPerWeek);

        var course = await _courseRepository.GetByIdAsync(id);
        var dept = dto.Department.Trim();
        var code = dto.Code.Trim();

        var conflict = await _courseRepository.FindByDepartmentAndCodeAsync(dept, code);
        if (conflict != null && conflict.Id != id)
            throw new InvalidOperationException($"Another course already uses department '{dept}' and code '{code}'.");

        course.Name = dto.Name.Trim();
        course.Department = dept;
        course.Code = code;
        course.Credits = dto.Credits;
        course.TimesPerWeek = dto.TimesPerWeek;

        await _courseRepository.UpdateAsync(course);
        return Map(course);
    }

    public async Task DeleteAsync(int id)
    {
        if (await _courseRepository.HasLessonsAsync(id))
            throw new InvalidOperationException("Cannot delete a course that still has lessons assigned.");

        var course = await _courseRepository.GetByIdAsync(id);
        await _courseRepository.RemoveAsync(course);
    }

    private static void ValidateCommon(string name, string department, string code, int credits, int timesPerWeek)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Course name is required.");
        if (string.IsNullOrWhiteSpace(department))
            throw new ArgumentException("Department is required.");
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Course code is required.");
        if (credits < 0)
            throw new ArgumentException("Credits cannot be negative.");
        if (timesPerWeek < 0)
            throw new ArgumentException("Times per week cannot be negative.");
    }

    private static CourseDto Map(Course c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Department = c.Department,
        Code = c.Code,
        Credits = c.Credits,
        TimesPerWeek = c.TimesPerWeek
    };
}
