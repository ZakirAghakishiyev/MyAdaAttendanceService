using System.Net.Http.Json;
using System.Net;
using System.Text.Json.Serialization;
using MyAdaAttendanceService.Application.Services.Interfaces;

namespace MyAdaAttendanceService.Application.Services;

public class AuthUserClient : IAuthUserClient
{
    private readonly HttpClient _httpClient;

    public AuthUserClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<AuthUserSummary>> GetUsersByRoleAsync(string role, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"api/auth/users-by-role/{Uri.EscapeDataString(role.Trim().ToLowerInvariant())}",
            cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            throw new UnauthorizedAccessException("Auth service denied the request. Provide an Authorization bearer token or configure AuthService service tokens.");

        if (response.StatusCode is HttpStatusCode.NotFound)
            throw new KeyNotFoundException("Auth service could not find users for the given role.");

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<UsersByRoleResponse>(cancellationToken);

        return payload?.Users?
            .Select(x => ToSummary(x, new[] { payload.Role ?? role }))
            .ToList() ?? new List<AuthUserSummary>();
    }

    public async Task<AuthUserSummary?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/auth/users/{userId}", cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            throw new UnauthorizedAccessException("Auth service denied the request. Provide an Authorization bearer token or configure AuthService service tokens.");

        if (response.StatusCode is HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<UserByIdResponse>(cancellationToken);

        return payload is null ? null : ToSummary(payload, payload.Roles);
    }

    private static AuthUserSummary ToSummary(BaseUserPayload payload, IEnumerable<string>? roles)
    {
        return new AuthUserSummary
        {
            Id = payload.Id,
            UserName = payload.UserName ?? string.Empty,
            Email = payload.Email ?? string.Empty,
            FirstName = payload.FirstName ?? string.Empty,
            LastName = payload.LastName ?? string.Empty,
            PhoneNumber = payload.PhoneNumber,
            UserType = payload.UserType,
            Status = payload.Status,
            Roles = roles?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim().ToLowerInvariant())
                .Distinct()
                .ToArray() ?? Array.Empty<string>()
        };
    }

    private sealed class UsersByRoleResponse
    {
        public string? Role { get; set; }
        public List<RoleUserPayload>? Users { get; set; }
    }

    private sealed class UserByIdResponse : BaseUserPayload
    {
        public List<string>? Roles { get; set; }
    }

    private sealed class RoleUserPayload : BaseUserPayload
    {
    }

    private abstract class BaseUserPayload
    {
        public Guid Id { get; set; }

        [JsonPropertyName("userName")]
        public string? UserName { get; set; }

        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? UserType { get; set; }
        public string? Status { get; set; }
    }
}
