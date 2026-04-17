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

Marks attendance by QR token for that student (same service as `qr/scan` below). If body `studentId` is empty, route `studentId` is used.

**Body:** `QrScanRequestDto`  
**Response:** `QrScanResponseDto`

---

### `POST /api/students/{studentId}/attendance/qr/scan`

Same behavior: validates QR token and records attendance for `studentId` from payload/route.

**Body — `QrScanRequestDto`:**

| Field         | Type    | Notes                    |
|---------------|---------|--------------------------|
| `studentId`   | guid    | Required logically; route id is used when omitted in body |
| `token`       | string  | Signed QR payload from instructor |
| `qrContext`   | object? | Optional context consistency checks |
| `deviceInfo`  | string? | Optional client metadata |

`qrContext` fields:

| Field           | Type    | Notes |
|-----------------|---------|-------|
| `sessionId`     | int?    | If present, must match token session |
| `roundCount`    | int?    | If present, must match token activation id |
| `instructorJwt` | string? | If present, instructor id in JWT must match session instructor |

**Response — `QrScanResponseDto`:**

| Field             | Type     | Notes                              |
|-------------------|----------|------------------------------------|
| `success`         | bool     |                                    |
| `errorCode`       | string?  | Machine-readable reject code on failures |
| `message`         | string   | Human-readable outcome             |
| `studentId`       | guid     | Resolved student id for this scan  |
| `sessionId`       | int      |                                    |
| `activationId`    | int?     |                                    |
| `validScanCount`  | int      |                                    |
| `status`          | string?  | e.g. attendance status when success |
| `scannedAt`       | datetime? |                                  |

**Typical errors:** invalid/expired token, session not found, activation inactive, outside attendance window, not enrolled, replay token.

---

## 2. Instructor (`/api/instructors/{instructorId}/sessions`)

Instructor endpoints use explicit `instructorId` route values.

Base path: **`/api/instructors/{instructorId}/sessions`**

### `POST /api/instructors/{instructorId}/sessions/{sessionId}/attendance/activate`

Opens attendance for the session (creates/activates an activation record, marks session attendance active).

**Response — `AttendanceActivationResultDto`:**

| Field                      | Type     |
|----------------------------|----------|
| `sessionId`                | int      |
| `isAttendanceActive`       | bool     |
| `attendanceActivatedAt`    | datetime? |
| `attendanceDeactivatedAt`  | datetime? |
| `message`                  | string   |

---

### `POST /api/instructors/{instructorId}/sessions/{sessionId}/attendance/deactivate`

Closes attendance for the session (deactivates activation, updates session flags).

**Response:** `AttendanceActivationResultDto`

---

### `POST /api/instructors/{instructorId}/sessions/{sessionId}/qr-token`

Issues a **short-lived signed JWT** (or similar) for display in a session QR code.

**Response — `QrTokenResponseDto`:**

| Field           | Type     |
|-----------------|----------|
| `sessionId`     | int      |
| `activationId`  | int      |
| `token`         | string   |
| `expiresAt`     | datetime |

Lifetime may be configured (e.g. `QrToken:LifetimeSeconds` in app settings).

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

Finalizes attendance for the session (see `AttendanceService.FinalizeAttendanceAsync`).

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
| `POST` | `/api/students/{studentId}/attendance/qr/scan` | Student |
| `POST` | `/api/instructors/{instructorId}/sessions/{sessionId}/attendance/activate` | Instructor |
| `POST` | `/api/instructors/{instructorId}/sessions/{sessionId}/attendance/deactivate` | Instructor |
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
