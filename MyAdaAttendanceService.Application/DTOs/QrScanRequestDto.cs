namespace MyAdaAttendanceService.Application.DTOs;

public class QrScanRequestDto
{
    public Guid StudentId { get; set; }

    /// <summary>
    /// HS256 QR JWT (session, activation, round, instructor, jti) from the instructor.
    /// Optional binding: append the student with <c>|{studentGuid}</c> (pipe + GUID) so the server can verify
    /// the same identity as the route: <c>{jwt}|aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee</c>.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    public string? DeviceInfo { get; set; }
}
