namespace MyAdaAttendanceService.Core.Entities;

public class AttendanceScanLog
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public int StudentId { get; set; }
    public int ActivationId { get; set; }
    public string TokenJti { get; set; } = string.Empty;
    public DateTime ScannedAt { get; set; }
    public bool Accepted { get; set; }
    public string? RejectReason { get; set; }
    public string? IpAddress { get; set; }
    public string? DeviceInfo { get; set; }
}
