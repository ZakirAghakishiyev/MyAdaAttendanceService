using System.ComponentModel.DataAnnotations;
using MyAdaAttendanceService.Core.Validation;

namespace MyAdaAttendanceService.Core.Entities;

public class ExternalUserDirectoryEntry
{
    public int Id { get; set; }

    [NonEmptyGuid]
    public Guid UserId { get; set; }

    [Required]
    [StringLength(64)]
    public string Role { get; set; } = string.Empty;

    [Required]
    [StringLength(256)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(64)]
    public string? PhoneNumber { get; set; }

    [StringLength(64)]
    public string? UserType { get; set; }

    [StringLength(64)]
    public string? Status { get; set; }

    public DateTime SyncedAtUtc { get; set; }
}
