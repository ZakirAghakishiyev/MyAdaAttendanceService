using System.ComponentModel.DataAnnotations;

namespace MyAdaAttendanceService.Core.Entities;

public class Course
{
    public int Id { get; set; }

    [Required]
    [StringLength(256)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(64)]
    public string Department { get; set; } = string.Empty;

    [Required]
    [StringLength(32)]
    public string Code { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int Credits { get; set; }

    [Range(0, int.MaxValue)]
    public int TimesPerWeek { get; set; }

    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
}
