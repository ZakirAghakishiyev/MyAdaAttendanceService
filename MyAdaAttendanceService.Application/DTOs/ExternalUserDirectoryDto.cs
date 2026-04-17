namespace MyAdaAttendanceService.Application.DTOs;

public class ExternalUserDirectoryDto
{
    public Guid UserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? UserType { get; set; }
    public string? Status { get; set; }
    public DateTime SyncedAtUtc { get; set; }
}
