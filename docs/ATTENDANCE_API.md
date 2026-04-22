# Attendance API

This document lists **all HTTP endpoints** that record, read, or manage attendance, across admin, instructor, student, and QR flows.

**Route user ids:** For `api/students/{studentId}` and `api/instructors/{instructorId}`, ids are auth-service user **GUIDs** passed in the route. In the current configuration, route/user auth matching checks are disabled.

**JSON enums:** `AcademicSemester` (where it appears on nested lesson payloads) uses `JsonStringEnumConverter`—prefer string values `"Fall"`, `"Spring"`, `"Summer"` in JSON.

**Attendance status strings** (used in DTOs as plain strings, not a JSON enum):

| Value     | Meaning   |
|-----------|-----------|
| `Present` | On time   |
| `Late`    | Late      |
| `Absent`  | Absent    |
| `Excused` | Excused   |

---

## 1. Student (`/api/students/{studentId}`)

### `GET /api/students/{studentId}/enrollments`

Lists enrolled lessons with **aggregated attendance counts** per lesson.

**Response:** `StudentLessonDto[]`

| Field            | Type   |
|------------------|--------|
| `lessonId`       | int    |
| `lessonName`     | string | Course title                         |
| `lessonCode`     | string | Course code                          |
| `totalSessions`  | int    |
| `presentCount`   | int    |
| `lateCount`      | int    |
| `absentCount`    | int    |
| `excusedCount`   | int    |

---

### `GET /api/students/{studentId}/lessons/{lessonId}/attendance`

Student’s **per-session** attendance rows for one lesson.

**Response:** `StudentAttendanceDto[]`

| Field               | Type     |
|---------------------|----------|
| `attendanceId`      | int      |
| `sessionId`         | int      |
| `sessionStartTime`  | datetime |
| `sessionEndTime`    | datetime |
| `lessonName`        | string   |
| `lessonCode`        | string   |
| `status`            | string   | `Present` / `Late` / `Absent` / `Excused` |
| `firstScanAt`       | datetime? |
| `lastScanAt`        | datetime? |
| `isManuallyAdjusted`| bool     |
| `instructorNote`    | string?  |

---

### `POST /api/students/{studentId}/attendance/scan`

Validates the **QR JWT** and records a scan for `studentId` (auth-service user **GUID** in the route). If body `studentId` is empty, the route `studentId` is used.

**Eligibility:** only students **enrolled in the session’s lesson** can scan successfully (`errorCode`: `student_not_enrolled` otherwise).

**Body — `QrScanRequestDto`:**

| Field         | Type    | Notes                    |
|---------------|---------|--------------------------|
| `studentId`   | guid    | Route id is used when omitted in body |
| `token`       | string  | See below |
| `deviceInfo`  | string? | Optional client metadata |

**`token` field**

- The value is a **signed HS256 JWT** issued by `POST .../qr-token` while a round (1 or 2) is active. All **session context** is inside the JWT (session id, activation id, **round** `1` or `2`, instructor id, `jti`, expiry). The client does not send separate `qrContext`.
- **Binding the user (recommended):** append a pipe and the same student’s GUID as in the request so the server can double-check:  
  `{jwt}|{studentGuid}`  
  If present, the GUID must match the route / body `studentId` or the scan is rejected (`student_token_mismatch`).

**JWT custom claims (reference):** `sid` (session id), `aid` (activation id), `rnd` (1 or 2), `ins` (instructor user id), plus standard `jti`, `exp`, `iat`. Issuer/audience default to `MyAda.Attendance.Qr` (overridable via `QrToken:Issuer` / `QrToken:Audience`).

**Attendance status from scans**

| Distinct successful rounds in this session (round 1 and/or 2) | `status` after scan / at finalize* |
|----------------------------------------------------------------|--------------------------------------|
| 2 (scanned in round 1 and round 2) | `Present` |
| 1 (only one round scanned) | `Late` |
| 0 | `Absent` (after finalize) |

*Live `status` updates after each successful scan; `POST .../attendance/finalize` recalculates the same rules for all enrolled students.

**Response — `QrScanResponseDto`:**

| Field             | Type     | Notes                              |
|-------------------|----------|------------------------------------|
| `success`         | bool     |                                    |
| `errorCode`       | string?  | Machine-readable reject code on failures |
| `message`         | string   | Human-readable outcome             |
| `studentId`       | guid     | Resolved student id for this scan  |
| `sessionId`       | int      |                                    |
| `activationId`    | int?     |                                    |
| `round`           | byte?    | `1` or `2` when success |
| `validScanCount`  | int      | **Distinct rounds** (1 or 2) completed for this student in this session |
| `status`          | string?  | e.g. `Present` / `Late` when success |
| `scannedAt`       | datetime? |                                  |

**Typical errors:** invalid/expired token, session not found, activation inactive, outside attendance window, not enrolled, replay token, already scanned in this round, student/token mismatch.

---

## 2. Instructor (`/api/instructors/{instructorId}/sessions`)

Instructor endpoints use explicit `instructorId` route values.

Base path: **`/api/instructors/{instructorId}/sessions`**

### `POST /api/instructors/{instructorId}/sessions/{sessionId}/attendance/activate/{round}`

Opens **one** attendance **round** for the session. `round` is **`1` or `2`**.

- **Round 1** — first check-in window (e.g. on-time / first half of class). Only if round 1 has not already been completed for this session.
- **Round 2** — second check-in window (e.g. late). Only after **round 1 is closed** (deactivated) and **round 2** has not already been completed.

At most one round can be active at a time. The general flow: `activate/1` → `qr-token` (repeat as the QR refreshes) → `deactivate/1` → `activate/2` → `qr-token` → `deactivate/2` → `finalize` (see below).

**Response — `AttendanceActivationResultDto`:** includes `round` (1 or 2).

| Field                      | Type     |
|----------------------------|----------|
| `sessionId`                | int      |
| `round`                    | byte?    | Which round was opened |
| `isAttendanceActive`       | bool     |
| `attendanceActivatedAt`    | datetime? |
| `attendanceDeactivatedAt`  | datetime? |
| `message`                  | string   |

---

### `POST /api/instructors/{instructorId}/sessions/{sessionId}/attendance/deactivate/{round}`

Closes the **currently active** round. `round` must match the active round (`1` or `2`). This does **not** run finalization; it only ends the activation so the next round can be opened (or the session can be finalized).

**Response:** `AttendanceActivationResultDto` (with `round` set).

---

### `POST /api/instructors/{instructorId}/sessions/{sessionId}/qr-token`

Issues a **short-lived HS256 JWT** (standard three-part `header.payload.sig`) for display in a QR while **some** round is active. The JWT encodes the session, activation, round, instructor, and `jti`.

**Response — `QrTokenResponseDto`:**

| Field           | Type     |
|-----------------|----------|
| `sessionId`     | int      |
| `activationId`  | int      |
| `round`         | byte     | `1` or `2` |
| `token`         | string   | JWT string |
| `expiresAt`     | datetime |

Configure `QrToken:Secret` (at least 32 effective bytes; shorter values are padded in development), `QrToken:Issuer`, `QrToken:Audience`, and `QrToken:LifetimeSeconds`.

---

### `GET /api/instructors/{instructorId}/sessions/{sessionId}/attendance`

Full **roster** for the session (instructor view).

**Response:** `AttendanceDto[]`

**`AttendanceDto` (main fields):**

| Field                 | Type     |
|-----------------------|----------|
| `id`                  | int      |
| `sessionId`         | int      |
| `lessonId`          | int      |
| `studentId`         | guid     |
| `studentFullName`   | string   |
| `studentCode`       | string   |
| `status`            | string   | `Present` / `Late` / `Absent` / `Excused` |
| `markedAt`          | datetime? |
| `markedSource`      | string?  |
| `updatedAt`         | datetime? |
| `updatedBy`         | guid?    |
| `firstScanAt`       | datetime? |
| `lastScanAt`        | datetime? |
| `isManuallyAdjusted`| bool     |
| `instructorNote`    | string?  |

---

### `GET /api/instructors/{instructorId}/sessions/{sessionId}/attendance/summary`

Rollup counts only (no per-student rows).

**Response — `AttendanceSummaryDto`:**

| Field           | Type |
|-----------------|------|
| `sessionId`     | int  |
| `totalStudents` | int  |
| `presentCount`  | int  |
| `lateCount`     | int  |
| `absentCount`   | int  |
| `excusedCount`  | int  |

---

### `PATCH /api/instructors/{instructorId}/sessions/{sessionId}/attendance/{studentId}`

Instructor updates one **enrolled student’s** attendance row for that session.

**Body — `UpdateAttendanceDto`:**

| Field             | Type    | Notes                                      |
|-------------------|---------|--------------------------------------------|
| `status`          | string  | `Present`, `Late`, `Absent`, or `Excused` |
| `instructorNote`  | string? | Optional                                   |

**Response:** `AttendanceDto`

---

### `POST /api/instructors/{instructorId}/sessions/{sessionId}/attendance/finalize`

Finalizes attendance for the session. **No** attendance round may be active: close the current round with `deactivate` first. For each **enrolled** student, `Present` / `Late` / `Absent` is set from the number of **distinct successful rounds (1 and 2)** they scanned, as in the table above. Rows that are manually adjusted are left unchanged.

**Body:** none  
**Response:** typically `200` / no content per wrapper.

---

### `POST /api/instructors/{instructorId}/sessions/{sessionId}/attendance/bulk-absent`

Marks **non-present** enrolled students as absent in bulk for that session.

**Body:** none  
**Response:** service-defined (check Swagger/runtime).

---

## 3. Admin (`/api/admin`)

Registrar/office. **Authentication/authorization** must allow these actions in your deployment.

### `GET /api/admin/sessions/{sessionId}/attendance`

Same roster shape as instructor **`AttendanceDto[]`**, without instructor ownership check (`GetSessionAttendanceAdminAsync`).

**Response:** `AttendanceDto[]`

---

### `PATCH /api/admin/sessions/{sessionId}/attendance/{attendanceId}`

Corrects a single attendance row by **attendance id**.

**Body — `AdminAttendanceCorrectionDto`:**

| Field    | Type    | Notes                                |
|----------|---------|--------------------------------------|
| `status` | string  | `Present`, `Late`, `Absent`, `Excused` |
| `note`   | string? | Optional                             |

**Response:** `AttendanceDto`

---

### `DELETE /api/admin/sessions/{sessionId}/attendance/{attendanceId}`

Deletes an attendance record.

**Response:** success per API wrapper conventions.

---

## 4. Related session listing (lessons)

Documented in **`ADMIN_API.md`**; paths use explicit ids:

| Method | Route | Notes |
|--------|--------|--------|
| `GET` | `/api/admin/lessons/{lessonId}/sessions` | Admin |
| `GET` | `/api/instructors/{instructorId}/lessons/{lessonId}/sessions` | Instructor |
| `GET` | `/api/instructors/{instructorId}/lessons/{lessonId}/sessions/{sessionId}` | Instructor |

`SessionDto` includes attendance rollups per session.

---

## Quick reference table

| Method | Path | Caller |
|--------|------|--------|
| `GET` | `/api/students/{studentId}/enrollments` | Student |
| `GET` | `/api/students/{studentId}/lessons/{lessonId}/attendance` | Student |
| `POST` | `/api/students/{studentId}/attendance/scan` | Student |
| `POST` | `/api/instructors/{instructorId}/sessions/{sessionId}/attendance/activate/{round}` | Instructor |
| `POST` | `/api/instructors/{instructorId}/sessions/{sessionId}/attendance/deactivate/{round}` | Instructor |
| `POST` | `/api/instructors/{instructorId}/sessions/{sessionId}/qr-token` | Instructor |
| `GET` | `/api/instructors/{instructorId}/sessions/{sessionId}/attendance` | Instructor |
| `GET` | `/api/instructors/{instructorId}/sessions/{sessionId}/attendance/summary` | Instructor |
| `PATCH` | `/api/instructors/{instructorId}/sessions/{sessionId}/attendance/{studentId}` | Instructor |
| `POST` | `/api/instructors/{instructorId}/sessions/{sessionId}/attendance/finalize` | Instructor |
| `POST` | `/api/instructors/{instructorId}/sessions/{sessionId}/attendance/bulk-absent` | Instructor |
| `GET` | `/api/admin/sessions/{sessionId}/attendance` | Admin |
| `PATCH` | `/api/admin/sessions/{sessionId}/attendance/{attendanceId}` | Admin |
| `DELETE` | `/api/admin/sessions/{sessionId}/attendance/{attendanceId}` | Admin |

---

## Document version

Aligned with `MyAdaAttendanceService.Web/Controllers` and application DTOs. Update when routes or contracts change.
