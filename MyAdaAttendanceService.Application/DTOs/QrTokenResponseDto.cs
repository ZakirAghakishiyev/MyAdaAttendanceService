namespace MyAdaAttendanceService.Application.DTOs;

public class QrTokenResponseDto
{
    public int SessionId { get; set; }
    public int ActivationId { get; set; }

    public byte Round { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
