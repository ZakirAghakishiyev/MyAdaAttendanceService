namespace MyAdaAttendanceService.Application.Services.Interfaces;

public interface IAuthUserClient
{
    Task<IReadOnlyList<AuthUserSummary>> GetUsersByRoleAsync(string role, CancellationToken cancellationToken = default);
    Task<AuthUserSummary?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed class AuthUserSummary
{
    public Guid Id { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string? UserType { get; init; }
    public string? Status { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
}
