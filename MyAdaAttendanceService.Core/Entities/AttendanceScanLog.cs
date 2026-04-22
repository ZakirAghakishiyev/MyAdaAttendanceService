using System.ComponentModel.DataAnnotations;
using MyAdaAttendanceService.Core.Validation;

namespace MyAdaAttendanceService.Core.Entities;

public class AttendanceScanLog
{
    public int Id { get; set; }

    [Range(1, int.MaxValue)]
    public int SessionId { get; set; }

    [NonEmptyGuid]
    public Guid StudentId { get; set; }

    [Range(0, int.MaxValue)]
    public int ActivationId { get; set; }

    /// <summary>1 or 2 when accepted; set when a JWT was parsed.</summary>
    public byte? Round { get; set; }

    [Required]
    [StringLength(128)]
    public string TokenJti { get; set; } = string.Empty;
    public DateTime ScannedAt { get; set; }
    public bool Accepted { get; set; }
    [StringLength(64)]
    public string? RejectReason { get; set; }

    [StringLength(64)]
    public string? IpAddress { get; set; }

    [StringLength(512)]
    public string? DeviceInfo { get; set; }
}
