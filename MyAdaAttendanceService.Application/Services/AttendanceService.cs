using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Application.Services.Interfaces;
using MyAdaAttendanceService.Core.Entities;
using MyAdaAttendanceService.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MyAdaAttendanceService.Application.Services;

public class AttendanceService : IAttendanceService
{
    private readonly ISessionAttendanceRepository _attendanceRepository;
    private readonly ILessonSessionRepository _sessionRepository;
    private readonly ILessonEnrollmentRepository _enrollmentRepository;
    private readonly IAttendanceActivationRepository _activationRepository;
    private readonly IAttendanceScanLogRepository _scanLogRepository;
    private readonly IExternalUserDirectoryService _userDirectoryService;
    private readonly IConfiguration _configuration;

    public AttendanceService(
        ISessionAttendanceRepository attendanceRepository,
        ILessonSessionRepository sessionRepository,
        ILessonEnrollmentRepository enrollmentRepository,
        IAttendanceActivationRepository activationRepository,
        IAttendanceScanLogRepository scanLogRepository,
        IExternalUserDirectoryService userDirectoryService,
        IConfiguration configuration)
    {
        _attendanceRepository = attendanceRepository;
        _sessionRepository = sessionRepository;
        _enrollmentRepository = enrollmentRepository;
        _activationRepository = activationRepository;
        _scanLogRepository = scanLogRepository;
        _userDirectoryService = userDirectoryService;
        _configuration = configuration;
    }

    public async Task<AttendanceActivationResultDto> ActivateAttendanceAsync(Guid instructorId, int sessionId)
    {
        var session = await GetVerifiedSessionAsync(instructorId, sessionId);
        var activeActivation = await _activationRepository.GetActiveBySessionIdAsync(sessionId);
        if (activeActivation is not null)
            throw new InvalidOperationException("Attendance is already active for this session.");

        var now = DateTime.UtcNow;
        var activation = new AttendanceActivation
        {
            SessionId = sessionId,
            StartedAt = now,
            IsActive = true,
            CreatedByInstructorId = instructorId
        };

        await _activationRepository.AddAsync(activation);

        session.IsAttendanceActive = true;
        session.AttendanceActivatedAt = now;
        session.AttendanceDeactivatedAt = null;
        await _sessionRepository.UpdateAsync(session);

        return new AttendanceActivationResultDto
        {
            SessionId = sessionId,
            IsAttendanceActive = true,
            AttendanceActivatedAt = now,
            Message = "Attendance activated successfully."
        };
    }

    public async Task<AttendanceActivationResultDto> DeactivateAttendanceAsync(Guid instructorId, int sessionId)
    {
        var session = await GetVerifiedSessionAsync(instructorId, sessionId);
        var activation = await _activationRepository.GetActiveBySessionIdAsync(sessionId);
        if (activation is null)
            throw new InvalidOperationException("Attendance is not active for this session.");

        await FinalizeAttendanceAsync(instructorId, sessionId);

        var now = DateTime.UtcNow;
        activation.IsActive = false;
        activation.EndedAt = now;
        await _activationRepository.UpdateAsync(activation);

        session.IsAttendanceActive = false;
        session.AttendanceDeactivatedAt = now;
        await _sessionRepository.UpdateAsync(session);

        return new AttendanceActivationResultDto
        {
            SessionId = sessionId,
            IsAttendanceActive = false,
            AttendanceActivatedAt = session.AttendanceActivatedAt,
            AttendanceDeactivatedAt = now,
            Message = "Attendance deactivated successfully."
        };
    }

    public async Task<QrTokenResponseDto> IssueQrTokenAsync(Guid instructorId, int sessionId)
    {
        _ = await GetVerifiedSessionAsync(instructorId, sessionId);
        var activation = await _activationRepository.GetActiveBySessionIdAsync(sessionId)
            ?? throw new KeyNotFoundException("Attendance is not active for this session.");

        var now = DateTime.UtcNow;
        var lifetimeSeconds = 15;
        if (int.TryParse(_configuration["QrToken:LifetimeSeconds"], out var parsed) && parsed > 0)
            lifetimeSeconds = parsed;
        var expiresAt = now.AddSeconds(lifetimeSeconds);
        var tokenPayload = new QrTokenPayload
        {
            SessionId = sessionId,
            ActivationId = activation.Id,
            Jti = Guid.NewGuid().ToString("N"),
            Iat = new DateTimeOffset(now).ToUnixTimeSeconds(),
            Exp = new DateTimeOffset(expiresAt).ToUnixTimeSeconds()
        };

        return new QrTokenResponseDto
        {
            SessionId = sessionId,
            ActivationId = activation.Id,
            Token = SignToken(tokenPayload),
            ExpiresAt = expiresAt
        };
    }

    public async Task<IEnumerable<AttendanceDto>> GetSessionAttendanceAsync(Guid instructorId, int sessionId)
    {
        var session = await GetVerifiedSessionAsync(instructorId, sessionId);
        var records = await _attendanceRepository.GetBySessionIdAsync(sessionId);
        var users = await _userDirectoryService.GetUsersByIdsAsync(records.Select(x => x.StudentId));
        return records.Select(r => MapToAttendanceDto(r, session.LessonId, users.GetValueOrDefault(r.StudentId)));
    }

    public async Task<IEnumerable<AttendanceDto>> GetSessionAttendanceAdminAsync(int sessionId)
    {
        var session = await _sessionRepository.GetByIdWithLessonAsync(sessionId)
            ?? throw new KeyNotFoundException($"Session {sessionId} not found.");

        var records = await _attendanceRepository.GetBySessionIdAsync(sessionId);
        var users = await _userDirectoryService.GetUsersByIdsAsync(records.Select(x => x.StudentId));
        return records.Select(r => MapToAttendanceDto(r, session.LessonId, users.GetValueOrDefault(r.StudentId)));
    }

    public async Task<AttendanceSummaryDto> GetSessionAttendanceSummaryAsync(Guid instructorId, int sessionId)
    {
        var session = await GetVerifiedSessionAsync(instructorId, sessionId);
        var records = await _attendanceRepository.GetBySessionIdAsync(sessionId);
        var enrollmentCount = (await _enrollmentRepository.GetByLessonIdAsync(session.LessonId)).Count;

        return new AttendanceSummaryDto
        {
            SessionId = sessionId,
            TotalStudents = enrollmentCount,
            PresentCount = records.Count(r => r.Status == AttendanceStatus.Present),
            LateCount = records.Count(r => r.Status == AttendanceStatus.Late),
            AbsentCount = records.Count(r => r.Status == AttendanceStatus.Absent),
            ExcusedCount = records.Count(r => r.Status == AttendanceStatus.Excused)
        };
    }

    public async Task<QrScanResponseDto> MarkAttendanceByQrAsync(QrScanRequestDto dto)
    {
        var studentId = dto.StudentId;
        var scannedAt = DateTime.UtcNow;
        if (studentId == Guid.Empty)
            return await RejectScanAsync(studentId, scannedAt, "student_id_missing", null, dto.DeviceInfo);

        if (string.IsNullOrWhiteSpace(dto.Token))
            return await RejectScanAsync(studentId, scannedAt, "token_missing", null, dto.DeviceInfo);

        if (!TryValidateToken(dto.Token, out var payload, out var rejectReason))
            return await RejectScanAsync(studentId, scannedAt, rejectReason ?? "invalid_token", payload, dto.DeviceInfo);

        var session = await _sessionRepository.GetByIdWithLessonAsync(payload!.SessionId);
        if (session is null)
            return await RejectScanAsync(studentId, scannedAt, "session_not_found", payload, dto.DeviceInfo);

        var activation = await _activationRepository.GetByIdAsync(payload.ActivationId);
        if (!activation.IsActive || activation.SessionId != payload.SessionId)
            return await RejectScanAsync(studentId, scannedAt, "activation_inactive", payload, dto.DeviceInfo);

        if (dto.QrContext?.SessionId is int contextSessionId && contextSessionId != payload.SessionId)
            return await RejectScanAsync(studentId, scannedAt, "context_session_mismatch", payload, dto.DeviceInfo);

        if (dto.QrContext?.RoundCount is int contextRoundCount && contextRoundCount != payload.ActivationId)
            return await RejectScanAsync(studentId, scannedAt, "context_round_mismatch", payload, dto.DeviceInfo);

        if (!ValidateInstructorSessionLinkage(dto.QrContext?.InstructorJwt, session))
            return await RejectScanAsync(studentId, scannedAt, "instructor_context_mismatch", payload, dto.DeviceInfo);

        var sessionStart = session.Date.ToDateTime(session.StartTime);
        var sessionEnd = session.Date.ToDateTime(session.EndTime);
        if (scannedAt < sessionStart.AddMinutes(-30) || scannedAt > sessionEnd.AddMinutes(30))
            return await RejectScanAsync(studentId, scannedAt, "outside_attendance_window", payload, dto.DeviceInfo);

        var isEnrolled = await _enrollmentRepository.ExistsAsync(session.LessonId, studentId);
        if (!isEnrolled)
            return await RejectScanAsync(studentId, scannedAt, "student_not_enrolled", payload, dto.DeviceInfo);

        var alreadyUsed = await _scanLogRepository.ExistsAcceptedByTokenAsync(payload.SessionId, studentId, payload.Jti);
        if (alreadyUsed)
            return await RejectScanAsync(studentId, scannedAt, "replay_token", payload, dto.DeviceInfo);

        await _scanLogRepository.AddAsync(new AttendanceScanLog
        {
            SessionId = payload.SessionId,
            StudentId = studentId,
            ActivationId = payload.ActivationId,
            TokenJti = payload.Jti,
            ScannedAt = scannedAt,
            Accepted = true,
            IpAddress = null,
            DeviceInfo = dto.DeviceInfo
        });

        var validScanCount = await _scanLogRepository.CountAcceptedScansAsync(payload.SessionId, studentId, payload.ActivationId);
        var computedStatus = validScanCount >= 2 ? AttendanceStatus.Present : AttendanceStatus.Late;
        var attendance = await _attendanceRepository.GetBySessionAndStudentAsync(payload.SessionId, studentId);

        if (attendance is null)
        {
            attendance = new SessionAttendance
            {
                SessionId = payload.SessionId,
                StudentId = studentId,
                Status = computedStatus,
                MarkedAt = scannedAt,
                MarkedSource = AttendanceMarkedSource.QR,
                UpdatedAt = scannedAt,
                UpdatedBy = studentId,
                FirstScanAt = scannedAt,
                LastScanAt = scannedAt
            };
            await _attendanceRepository.AddAsync(attendance);
        }
        else
        {
            attendance.LastScanAt = scannedAt;
            attendance.UpdatedAt = scannedAt;
            attendance.UpdatedBy = studentId;
            if (!attendance.IsManuallyAdjusted)
            {
                attendance.Status = computedStatus;
                attendance.MarkedSource = AttendanceMarkedSource.QR;
                attendance.MarkedAt ??= scannedAt;
            }
            attendance.FirstScanAt ??= scannedAt;
            await _attendanceRepository.UpdateAsync(attendance);
        }

        return new QrScanResponseDto
        {
            Success = true,
            Message = "Scan accepted.",
            StudentId = studentId,
            SessionId = payload.SessionId,
            ActivationId = payload.ActivationId,
            ValidScanCount = validScanCount,
            Status = attendance.Status.ToString(),
            ScannedAt = scannedAt
        };
    }

    public async Task<AttendanceDto> UpdateAttendanceAsync(Guid instructorId, int sessionId, Guid studentId, UpdateAttendanceDto dto)
    {
        var session = await _sessionRepository.GetByIdWithLessonAsync(sessionId)
            ?? throw new KeyNotFoundException("Session not found.");

        if (session.Lesson!.InstructorId != instructorId)
            throw new UnauthorizedAccessException("You do not own this session.");

        if (!Enum.TryParse<AttendanceStatus>(dto.Status, true, out var status))
            throw new ArgumentException($"Invalid attendance status: {dto.Status}");

        var attendance = await _attendanceRepository.GetBySessionAndStudentAsync(sessionId, studentId)
            ?? new SessionAttendance
            {
                SessionId = sessionId,
                StudentId = studentId
            };

        attendance.Status = status;
        attendance.InstructorNote = dto.InstructorNote;
        attendance.IsManuallyAdjusted = true;
        attendance.MarkedAt = DateTime.UtcNow;
        attendance.MarkedSource = AttendanceMarkedSource.Manual;
        attendance.UpdatedAt = DateTime.UtcNow;
        attendance.UpdatedBy = instructorId;

        if (attendance.Id == 0)
            await _attendanceRepository.AddAsync(attendance);
        else
            await _attendanceRepository.UpdateAsync(attendance);

        var user = await _userDirectoryService.GetUserByIdAsync(studentId);
        return MapToAttendanceDto(attendance, session.LessonId, user);
    }

    public async Task FinalizeAttendanceAsync(Guid instructorId, int sessionId)
    {
        var session = await GetVerifiedSessionAsync(instructorId, sessionId);
        var activation = await _activationRepository.GetActiveBySessionIdAsync(sessionId);
        if (activation is null)
        {
            var latestActivation = await _activationRepository.GetAllAsync(
                predicate: x => x.SessionId == sessionId,
                orderBy: q => q.OrderByDescending(x => x.StartedAt));
            activation = latestActivation.FirstOrDefault();
        }
        if (activation is null)
            throw new KeyNotFoundException("No attendance activation exists for this session.");

        var acceptedScans = await _scanLogRepository.GetAcceptedBySessionAndActivationAsync(sessionId, activation.Id);
        var acceptedCounts = acceptedScans
            .GroupBy(x => x.StudentId)
            .ToDictionary(g => g.Key, g => g.Count());
        var enrollments = await _enrollmentRepository.GetByLessonIdAsync(session.LessonId);

        foreach (var enrollment in enrollments)
        {
            acceptedCounts.TryGetValue(enrollment.StudentId, out var count);
            var computedStatus = count >= 2
                ? AttendanceStatus.Present
                : (count == 1 ? AttendanceStatus.Late : AttendanceStatus.Absent);

            var attendance = await _attendanceRepository.GetBySessionAndStudentAsync(sessionId, enrollment.StudentId);
            if (attendance is null)
            {
                attendance = new SessionAttendance
                {
                    SessionId = sessionId,
                    StudentId = enrollment.StudentId,
                    Status = computedStatus,
                    MarkedAt = DateTime.UtcNow,
                    MarkedSource = AttendanceMarkedSource.QR,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = instructorId
                };
                await _attendanceRepository.AddAsync(attendance);
            }
            else if (!attendance.IsManuallyAdjusted)
            {
                attendance.Status = computedStatus;
                attendance.MarkedSource = AttendanceMarkedSource.QR;
                attendance.MarkedAt ??= DateTime.UtcNow;
                attendance.UpdatedAt = DateTime.UtcNow;
                attendance.UpdatedBy = instructorId;
                await _attendanceRepository.UpdateAsync(attendance);
            }
        }
    }

    public async Task BulkMarkAbsentAsync(Guid instructorId, int sessionId)
    {
        var session = await GetVerifiedSessionAsync(instructorId, sessionId);

        var enrollments = await _enrollmentRepository.GetByLessonIdAsync(session.LessonId);
        var existingAttendances = await _attendanceRepository.GetBySessionIdAsync(sessionId);
        var studentsWithAttendance = existingAttendances.Select(a => a.StudentId).ToHashSet();

        foreach (var enrollment in enrollments)
        {
            if (studentsWithAttendance.Contains(enrollment.StudentId))
                continue;

            var absent = new SessionAttendance
            {
                SessionId = sessionId,
                StudentId = enrollment.StudentId,
                Status = AttendanceStatus.Absent
            };

            await _attendanceRepository.AddAsync(absent);
        }
    }

    private async Task<LessonSession> GetVerifiedSessionAsync(Guid instructorId, int sessionId)
    {
        var session = await _sessionRepository.GetByIdWithLessonAsync(sessionId)
            ?? throw new KeyNotFoundException($"Session {sessionId} not found.");

        if (session.Lesson!.InstructorId != instructorId)
            throw new UnauthorizedAccessException("You do not own this session.");

        return session;
    }

    private async Task<QrScanResponseDto> RejectScanAsync(
        Guid studentId,
        DateTime scannedAt,
        string reason,
        QrTokenPayload? payload,
        string? deviceInfo)
    {
        await _scanLogRepository.AddAsync(new AttendanceScanLog
        {
            SessionId = payload?.SessionId ?? 0,
            StudentId = studentId,
            ActivationId = payload?.ActivationId ?? 0,
            TokenJti = payload?.Jti ?? string.Empty,
            ScannedAt = scannedAt,
            Accepted = false,
            RejectReason = reason,
            IpAddress = null,
            DeviceInfo = deviceInfo
        });

        return new QrScanResponseDto
        {
            Success = false,
            ErrorCode = reason,
            Message = MapRejectReason(reason),
            StudentId = studentId,
            SessionId = payload?.SessionId ?? 0,
            ActivationId = payload?.ActivationId,
            ValidScanCount = 0,
            ScannedAt = scannedAt
        };
    }

    private static string MapRejectReason(string reason) => reason switch
    {
        "student_id_missing" => "Student id is required.",
        "token_missing" => "QR token is required.",
        "invalid_token_format" => "QR token format is invalid.",
        "invalid_token_signature" => "QR token signature is invalid.",
        "invalid_token_payload" => "QR token payload is invalid.",
        "token_expired" => "QR token has expired.",
        "session_not_found" => "Session from QR token was not found.",
        "activation_inactive" => "Attendance round is inactive.",
        "context_session_mismatch" => "QR context session does not match token session.",
        "context_round_mismatch" => "QR context round does not match active attendance round.",
        "instructor_context_mismatch" => "QR instructor context does not match this session instructor.",
        "outside_attendance_window" => "Scan is outside the attendance time window.",
        "student_not_enrolled" => "Student is not enrolled in this lesson.",
        "replay_token" => "This QR token was already used by this student.",
        _ => "Attendance scan could not be processed."
    };

    private string SignToken(QrTokenPayload payload)
    {
        var payloadJson = JsonSerializer.Serialize(payload);
        var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
        var payloadPart = Base64UrlEncode(payloadBytes);
        var signature = ComputeSignature(payloadPart);
        return $"{payloadPart}.{signature}";
    }

    private bool TryValidateToken(string token, out QrTokenPayload? payload, out string? error)
    {
        payload = null;
        error = null;
        var parts = token.Split('.');
        if (parts.Length != 2)
        {
            error = "invalid_token_format";
            return false;
        }

        var payloadPart = parts[0];
        var signaturePart = parts[1];
        var expectedSignature = ComputeSignature(payloadPart);
        if (!FixedTimeEquals(signaturePart, expectedSignature))
        {
            error = "invalid_token_signature";
            return false;
        }

        try
        {
            var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(payloadPart));
            payload = JsonSerializer.Deserialize<QrTokenPayload>(payloadJson);
            if (payload is null)
            {
                error = "invalid_token_payload";
                return false;
            }
            var nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (payload.Exp <= nowUnix)
            {
                error = "token_expired";
                return false;
            }
            return true;
        }
        catch
        {
            error = "invalid_token_payload";
            return false;
        }
    }

    private string ComputeSignature(string payloadPart)
    {
        var secret = _configuration["QrToken:Secret"]
            ?? "change_this_secret_in_configuration";
        var secretBytes = Encoding.UTF8.GetBytes(secret);
        using var hmac = new HMACSHA256(secretBytes);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadPart));
        return Base64UrlEncode(hash);
    }

    private static string Base64UrlEncode(byte[] input) =>
        Convert.ToBase64String(input)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static byte[] Base64UrlDecode(string input)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }
        return Convert.FromBase64String(padded);
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private static bool ValidateInstructorSessionLinkage(string? instructorJwt, LessonSession session)
    {
        if (string.IsNullOrWhiteSpace(instructorJwt))
            return true;

        if (session.Lesson is null)
            return false;

        return TryExtractGuidFromJwt(instructorJwt, out var instructorIdFromToken)
            && instructorIdFromToken == session.Lesson.InstructorId;
    }

    private static bool TryExtractGuidFromJwt(string jwt, out Guid userId)
    {
        userId = Guid.Empty;
        var parts = jwt.Split('.');
        if (parts.Length < 2)
            return false;

        try
        {
            var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
            using var document = JsonDocument.Parse(payloadJson);
            var root = document.RootElement;

            if (TryGetGuidClaim(root, "sub", out userId) ||
                TryGetGuidClaim(root, "nameid", out userId) ||
                TryGetGuidClaim(root, "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", out userId))
            {
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryGetGuidClaim(JsonElement root, string claimName, out Guid value)
    {
        value = Guid.Empty;
        if (!root.TryGetProperty(claimName, out var claimElement) || claimElement.ValueKind != JsonValueKind.String)
            return false;

        return Guid.TryParse(claimElement.GetString(), out value);
    }

    private static AttendanceDto MapToAttendanceDto(
        SessionAttendance record,
        int lessonId,
        ExternalUserDirectoryDto? student = null) => new()
    {
        Id = record.Id,
        SessionId = record.SessionId,
        LessonId = lessonId,
        StudentId = record.StudentId,
        StudentFullName = student?.DisplayName ?? string.Empty,
        StudentCode = student?.UserName ?? string.Empty,
        Status = record.Status.ToString(),
        MarkedAt = record.MarkedAt,
        MarkedSource = record.MarkedSource.ToString(),
        UpdatedAt = record.UpdatedAt,
        UpdatedBy = record.UpdatedBy,
        FirstScanAt = record.FirstScanAt,
        LastScanAt = record.LastScanAt,
        IsManuallyAdjusted = record.IsManuallyAdjusted,
        InstructorNote = record.InstructorNote
    };

    private sealed class QrTokenPayload
    {
        public int SessionId { get; set; }
        public int ActivationId { get; set; }
        public string Jti { get; set; } = string.Empty;
        public long Iat { get; set; }
        public long Exp { get; set; }
    }
}
