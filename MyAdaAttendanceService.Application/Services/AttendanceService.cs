using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Application.Services.Interfaces;
using MyAdaAttendanceService.Core.Entities;
using MyAdaAttendanceService.Core.Interfaces;

namespace MyAdaAttendanceService.Application.Services;

public class AttendanceService : IAttendanceService
{
    private const string ClaimSession = "sid";
    private const string ClaimActivation = "aid";
    private const string ClaimRound = "rnd";
    private const string ClaimInstructor = "ins";
    private const string DefaultIssuer = "MyAda.Attendance.Qr";
    private const string DefaultAudience = "MyAda.Attendance.Qr";

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

    public async Task<AttendanceActivationResultDto> ActivateAttendanceForRoundAsync(
        Guid instructorId,
        int sessionId,
        int round)
    {
        if (round is < 1 or > 2)
            throw new ArgumentException("Round must be 1 or 2.");

        var session = await GetVerifiedSessionAsync(instructorId, sessionId);
        if (await _activationRepository.GetActiveBySessionIdAsync(sessionId) is not null)
            throw new InvalidOperationException("A round is already active. Close it before starting another.");

        if (round == 1)
        {
            if (await _activationRepository.HasClosedActivationForRoundAsync(sessionId, 1))
                throw new InvalidOperationException("Round 1 was already completed for this session.");
        }
        else
        {
            if (!await _activationRepository.HasClosedActivationForRoundAsync(sessionId, 1))
                throw new InvalidOperationException("Close round 1 before activating round 2.");
            if (await _activationRepository.HasClosedActivationForRoundAsync(sessionId, 2))
                throw new InvalidOperationException("Round 2 was already completed for this session.");
        }

        var now = DateTime.UtcNow;
        var activation = new AttendanceActivation
        {
            SessionId = sessionId,
            Round = (byte)round,
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
            Round = (byte)round,
            IsAttendanceActive = true,
            AttendanceActivatedAt = now,
            Message = round == 1
                ? "Round 1 (on-time) attendance is active."
                : "Round 2 (late) attendance is active."
        };
    }

    public async Task<AttendanceActivationResultDto> DeactivateAttendanceForRoundAsync(
        Guid instructorId,
        int sessionId,
        int round)
    {
        if (round is < 1 or > 2)
            throw new ArgumentException("Round must be 1 or 2.");

        var session = await GetVerifiedSessionAsync(instructorId, sessionId);
        var active = await _activationRepository.GetActiveBySessionIdAsync(sessionId);
        if (active is null)
            throw new InvalidOperationException("No round is currently active for this session.");
        if (active.Round != round)
            throw new InvalidOperationException($"The active round is {active.Round}, not {round}.");

        var now = DateTime.UtcNow;
        active.IsActive = false;
        active.EndedAt = now;
        await _activationRepository.UpdateAsync(active);

        session.IsAttendanceActive = false;
        session.AttendanceDeactivatedAt = now;
        await _sessionRepository.UpdateAsync(session);

        return new AttendanceActivationResultDto
        {
            SessionId = sessionId,
            Round = (byte)round,
            IsAttendanceActive = false,
            AttendanceActivatedAt = session.AttendanceActivatedAt,
            AttendanceDeactivatedAt = now,
            Message = "Attendance round closed."
        };
    }

    public async Task<QrTokenResponseDto> IssueQrTokenAsync(Guid instructorId, int sessionId)
    {
        var session = await GetVerifiedSessionAsync(instructorId, sessionId);
        var activation = await _activationRepository.GetActiveBySessionIdAsync(sessionId)
            ?? throw new KeyNotFoundException("No attendance round is active for this session. Activate round 1 or 2 first.");

        var now = DateTime.UtcNow;
        var lifetimeSeconds = 15;
        if (int.TryParse(_configuration["QrToken:LifetimeSeconds"], out var parsed) && parsed > 0)
            lifetimeSeconds = parsed;
        var expiresAt = now.AddSeconds(lifetimeSeconds);
        var jti = Guid.NewGuid().ToString("N");

        var token = CreateQrJwt(
            new QrTokenPayloadModel
            {
                Jti = jti,
                SessionId = sessionId,
                ActivationId = activation.Id,
                Round = activation.Round,
                InstructorId = instructorId
            },
            expiresAt);

        return new QrTokenResponseDto
        {
            SessionId = sessionId,
            ActivationId = activation.Id,
            Round = activation.Round,
            Token = token,
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

        var (jwtPortion, appendedStudentId) = ParseBoundStudentFromToken(dto.Token);
        if (string.IsNullOrWhiteSpace(jwtPortion))
            return await RejectScanAsync(studentId, scannedAt, "token_missing", null, dto.DeviceInfo);
        if (appendedStudentId is { } appId && appId != studentId)
            return await RejectScanAsync(studentId, scannedAt, "student_token_mismatch", null, dto.DeviceInfo);

        if (!TryValidateQrJwt(jwtPortion, out var tokenPayload, out var rejectReason))
            return await RejectScanAsync(studentId, scannedAt, rejectReason ?? "invalid_token", null, dto.DeviceInfo);

        var session = await _sessionRepository.GetByIdWithLessonAsync(tokenPayload!.SessionId);
        if (session is null)
            return await RejectScanAsync(studentId, scannedAt, "session_not_found", tokenPayload, dto.DeviceInfo);

        if (session.Lesson!.InstructorId != tokenPayload.InstructorId)
            return await RejectScanAsync(studentId, scannedAt, "instructor_mismatch", tokenPayload, dto.DeviceInfo);

        var activation = await _activationRepository.GetByIdAsync(tokenPayload.ActivationId);
        if (activation is null
            || !activation.IsActive
            || activation.SessionId != tokenPayload.SessionId
            || activation.Round != tokenPayload.Round)
        {
            return await RejectScanAsync(studentId, scannedAt, "activation_inactive", tokenPayload, dto.DeviceInfo);
        }

        var sessionStart = session.Date.ToDateTime(session.StartTime);
        var sessionEnd = session.Date.ToDateTime(session.EndTime);
        if (scannedAt < sessionStart.AddMinutes(-30) || scannedAt > sessionEnd.AddMinutes(30))
            return await RejectScanAsync(studentId, scannedAt, "outside_attendance_window", tokenPayload, dto.DeviceInfo);

        var isEnrolled = await _enrollmentRepository.ExistsAsync(session.LessonId, studentId);
        if (!isEnrolled)
            return await RejectScanAsync(studentId, scannedAt, "student_not_enrolled", tokenPayload, dto.DeviceInfo);

        if (await _scanLogRepository.HasAcceptedScanInRoundAsync(
                tokenPayload.SessionId,
                studentId,
                tokenPayload.Round))
        {
            return await RejectScanAsync(studentId, scannedAt, "already_scanned_this_round", tokenPayload, dto.DeviceInfo);
        }

        var alreadyUsedJti = await _scanLogRepository.ExistsAcceptedByTokenAsync(
            tokenPayload.SessionId,
            studentId,
            tokenPayload.Jti);
        if (alreadyUsedJti)
            return await RejectScanAsync(studentId, scannedAt, "replay_token", tokenPayload, dto.DeviceInfo);

        await _scanLogRepository.AddAsync(new AttendanceScanLog
        {
            SessionId = tokenPayload.SessionId,
            StudentId = studentId,
            ActivationId = tokenPayload.ActivationId,
            Round = tokenPayload.Round,
            TokenJti = tokenPayload.Jti,
            ScannedAt = scannedAt,
            Accepted = true,
            IpAddress = null,
            DeviceInfo = dto.DeviceInfo
        });

        var distinctRounds = await _scanLogRepository.CountDistinctRoundsScannedAsync(tokenPayload.SessionId, studentId);
        var computedStatus = distinctRounds >= 2
            ? AttendanceStatus.Present
            : AttendanceStatus.Late;

        var attendance = await _attendanceRepository.GetBySessionAndStudentAsync(tokenPayload.SessionId, studentId);
        if (attendance is null)
        {
            attendance = new SessionAttendance
            {
                SessionId = tokenPayload.SessionId,
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
            SessionId = tokenPayload.SessionId,
            ActivationId = tokenPayload.ActivationId,
            Round = tokenPayload.Round,
            ValidScanCount = distinctRounds,
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
        if (await _activationRepository.GetActiveBySessionIdAsync(sessionId) is not null)
            throw new InvalidOperationException("An attendance round is still active. Deactivate the current round before finalizing.");

        var accepted = await _scanLogRepository.GetAcceptedBySessionIdAsync(sessionId);
        var roundsByStudent = accepted
            .GroupBy(x => x.StudentId)
            .ToDictionary(g => g.Key, g => g.Select(s => s.Round).Distinct().Count());
        var enrollments = await _enrollmentRepository.GetByLessonIdAsync(session.LessonId);

        foreach (var enrollment in enrollments)
        {
            roundsByStudent.TryGetValue(enrollment.StudentId, out var roundCount);
            var computedStatus = roundCount >= 2
                ? AttendanceStatus.Present
                : (roundCount == 1 ? AttendanceStatus.Late : AttendanceStatus.Absent);

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
        QrTokenPayloadModel? payload,
        string? deviceInfo)
    {
        if (payload is not null)
        {
            await _scanLogRepository.AddAsync(new AttendanceScanLog
            {
                SessionId = payload.SessionId,
                StudentId = studentId,
                ActivationId = payload.ActivationId,
                Round = payload.Round,
                TokenJti = payload.Jti,
                ScannedAt = scannedAt,
                Accepted = false,
                RejectReason = reason,
                IpAddress = null,
                DeviceInfo = deviceInfo
            });
        }

        return new QrScanResponseDto
        {
            Success = false,
            ErrorCode = reason,
            Message = MapRejectReason(reason),
            StudentId = studentId,
            SessionId = payload?.SessionId ?? 0,
            ActivationId = payload?.ActivationId,
            Round = payload?.Round,
            ValidScanCount = 0,
            ScannedAt = scannedAt
        };
    }

    private static string MapRejectReason(string reason) => reason switch
    {
        "student_id_missing" => "Student id is required.",
        "token_missing" => "QR token is required.",
        "student_token_mismatch" => "The bound student in the token does not match the request.",
        "invalid_token" => "QR token could not be read.",
        "invalid_token_format" => "QR token format is invalid.",
        "invalid_token_signature" => "QR token signature is invalid.",
        "invalid_token_payload" => "QR token payload is invalid.",
        "token_expired" => "QR token has expired.",
        "session_not_found" => "Session from QR token was not found.",
        "instructor_mismatch" => "QR token instructor does not match the session owner.",
        "activation_inactive" => "Attendance round is inactive or does not match the token.",
        "already_scanned_this_round" => "This student already scanned in this round.",
        "outside_attendance_window" => "Scan is outside the attendance time window.",
        "student_not_enrolled" => "Student is not enrolled in this lesson.",
        "replay_token" => "This QR token was already used by this student.",
        _ => "Attendance scan could not be processed."
    };

    private string CreateQrJwt(QrTokenPayloadModel payload, DateTime expiresAtUtc)
    {
        var key = GetSymmetricSecurityKey();
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var now = DateTime.UtcNow;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, payload.Jti),
            new(ClaimSession, payload.SessionId.ToString()),
            new(ClaimActivation, payload.ActivationId.ToString()),
            new(ClaimRound, payload.Round.ToString()),
            new(ClaimInstructor, payload.InstructorId.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: GetQrIssuer(),
            audience: GetQrAudience(),
            claims: claims,
            notBefore: now,
            expires: expiresAtUtc,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private bool TryValidateQrJwt(string token, out QrTokenPayloadModel? model, out string? error)
    {
        model = null;
        error = null;
        try
        {
            var key = GetSymmetricSecurityKey();
            var tokenHandler = new JwtSecurityTokenHandler();
            var validation = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = GetQrIssuer(),
                ValidateAudience = true,
                ValidAudience = GetQrAudience(),
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };
            var principal = tokenHandler.ValidateToken(token, validation, out var vToken);
            var jwt = (JwtSecurityToken)vToken;

            if (!int.TryParse(jwt.Claims.FirstOrDefault(c => c.Type == ClaimSession)?.Value, out var sessionId) ||
                !int.TryParse(jwt.Claims.FirstOrDefault(c => c.Type == ClaimActivation)?.Value, out var activationId) ||
                !byte.TryParse(jwt.Claims.FirstOrDefault(c => c.Type == ClaimRound)?.Value, out var round) ||
                round is < 1 or > 2 ||
                !Guid.TryParse(jwt.Claims.FirstOrDefault(c => c.Type == ClaimInstructor)?.Value, out var instructorId))
            {
                error = "invalid_token_payload";
                return false;
            }

            var jti = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
            if (string.IsNullOrEmpty(jti))
            {
                error = "invalid_token_payload";
                return false;
            }

            model = new QrTokenPayloadModel
            {
                Jti = jti,
                SessionId = sessionId,
                ActivationId = activationId,
                Round = round,
                InstructorId = instructorId
            };
            return true;
        }
        catch (Exception)
        {
            error = "invalid_token";
            return false;
        }
    }

    private SymmetricSecurityKey GetSymmetricSecurityKey()
    {
        var secret = _configuration["QrToken:Secret"] ?? "change_this_secret_in_configuration";
        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("QrToken:Secret must be configured.");
        if (secret.Length < 32)
            secret = secret.PadRight(32, 'x');

        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
    }

    private string GetQrIssuer() => _configuration["QrToken:Issuer"] ?? DefaultIssuer;
    private string GetQrAudience() => _configuration["QrToken:Audience"] ?? DefaultAudience;

    private static (string JwtPortion, Guid? AppendedStudentId) ParseBoundStudentFromToken(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return (string.Empty, null);
        var s = raw.Trim();
        var pipe = s.LastIndexOf('|');
        if (pipe <= 0)
            return (s, null);
        var tail = s[(pipe + 1)..].Trim();
        if (Guid.TryParse(tail, out var g))
            return (s[..pipe].Trim(), g);
        return (s, null);
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

    private sealed class QrTokenPayloadModel
    {
        public int SessionId { get; set; }
        public int ActivationId { get; set; }
        public byte Round { get; set; }
        public Guid InstructorId { get; set; }
        public string Jti { get; set; } = string.Empty;
    }
}
