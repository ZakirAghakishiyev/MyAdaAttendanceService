# Admin API (`/api/admin`)

Base path: **`/api/admin`**.

JSON serialization uses **`JsonStringEnumConverter`**, so enums such as **`AcademicSemester`** are typically sent and returned as **strings** (`"Fall"`, `"Spring"`, `"Summer"`) in JSON. Numeric values `1`, `2`, `3` match the underlying enum integers.

Responses may be wrapped by **AutoWrapper** (envelope with `result`, `statusCode`, etc.) depending on host configuration—check a live response or Swagger for the exact shape.

**Instructor & student APIs (not under `/api/admin`):** Lesson and session operations use explicit ids in the path, for example `GET /api/instructors/{instructorId}/lessons` and `GET /api/students/{studentId}/enrollments`. These ids are auth-service user **GUIDs**. Details for attendance and session listing are in **`ATTENDANCE_API.md`**.

---

## Courses

### `GET /api/admin/courses`

Lists all courses (ordered by department, then code).

**Response:** `CourseDto[]`

---

### `GET /api/admin/courses/{courseId}`

Returns one course by id.

**Response:** `CourseDto`  
**Errors:** 404 if not found.

---

### `POST /api/admin/courses`

Creates a course.

**Body — `CreateCourseDto`:**

| Field          | Type   | Notes                          |
|----------------|--------|--------------------------------|
| `name`         | string | Catalog title                  |
| `department`   | string |                                |
| `code`         | string | Unique together with department |
| `credits`      | int    | ≥ 0                            |
| `timesPerWeek` | int    | ≥ 0                            |

**Response:** `201 Created`, body `CourseDto`, header `Location: /api/admin/courses/{id}`

---

### `PUT /api/admin/courses/{courseId}`

Updates a course.

**Body — `UpdateCourseDto`:** same fields as `CreateCourseDto`.

---

### `DELETE /api/admin/courses/{courseId}`

Deletes a course (may fail if lessons or other rules block deletion).

---

### `GET /api/admin/courses/{courseId}/lessons`

Returns all lessons for that course.

**Response:** `LessonDto[]` (summary list; typically without nested `sessions`).

---

## Auth user directory

The attendance service can sync instructors/students from the auth microservice into a local directory cache. In the current ID-only validation mode, missing users may also be inserted as local placeholder rows.

### `POST /api/admin/users/sync`

Pulls users from the auth service and upserts them into the local directory.

**Query params:** `roles` (optional, repeatable). Default roles are `student` and `instructor`.

**Response — `UserSyncResultDto`:**

| Field           | Type       |
|----------------|------------|
| `importedCount` | int       |
| `roles`         | string[]   |
| `syncedAtUtc`   | datetime   |

---

### `GET /api/admin/users/roles/{role}`

Returns synced users for the requested role.

**Response — `ExternalUserDirectoryDto[]`:**

| Field          | Type     |
|----------------|----------|
| `userId`       | guid     |
| `role`         | string   |
| `userName`     | string   |
| `email`        | string   |
| `firstName`    | string   |
| `lastName`     | string   |
| `displayName`  | string   |
| `phoneNumber`  | string?  |
| `userType`     | string?  |
| `status`       | string?  |
| `syncedAtUtc`  | datetime |

---

## Lessons

### `GET /api/admin/lessons`

Lists all lessons.

**Response:** `LessonDto[]` (summary; no `sessions`).

---

### `GET /api/admin/lessons/by-term?academicYear={year}&semester={semester}`

Lists lessons by academic term.

**Query params:**

| Field          | Type               | Notes |
|----------------|--------------------|-------|
| `academicYear` | int                | Required; valid range 2000..2100 |
| `semester`     | `AcademicSemester` | Required; `Fall`, `Spring`, or `Summer` |

**Response:** `LessonDto[]` (summary; no `sessions`).

---

### `GET /api/admin/lessons/scheduling`

Scheduling integration: instructor user id, enrollment headcount, weekly meeting count, course labels.

**Response:** `LessonSchedulingRow[]`

| Field              | Type   | Description                                      |
|--------------------|--------|--------------------------------------------------|
| `lessonId`         | int    |                                                  |
| `instructorUserId` | guid   | Instructor auth user id                          |
| `enrollment`       | int    | Count of enrollment rows for the lesson          |
| `maxCapacity`      | int    | Section capacity                                 |
| `timesPerWeek`     | int    | From the linked course                           |
| `courseCode`       | string | Course code                                      |
| `courseTitle`      | string | Course name / title                              |

---

### `GET /api/admin/lessons/{lessonId}`

Lesson detail, including ordered sessions when loaded from the service.

**Response:** `LessonDto` (often includes `sessions`).

---

### `POST /api/admin/lessons`

Creates a lesson for an **existing** course. **Do not send `crn`** (JSON camelCase for `CRN`)—the server generates it.

**Body — `CreateLessonDto`:**

| Field           | Type               | Notes                                                |
|-----------------|--------------------|------------------------------------------------------|
| `courseId`      | int                | Required; course must exist                          |
| `instructorId`  | guid               | Assigned instructor auth user id                     |
| `roomId`        | int                |                                                      |
| `academicYear`  | int                | Calendar year for the term (e.g. `2026` for Spring 2026); validated ~2000–2100 |
| `semester`      | `AcademicSemester` | See enum below                                       |
| `maxCapacity`   | int                | ≥ 0                                                  |

**Response:** `201 Created`, body `LessonDto`, header `Location: /api/admin/lessons/{id}`

---

### `PUT /api/admin/lessons/{lessonId}`

Updates a lesson. If **term** (`academicYear` / `semester`) changes, the server may assign a **new CRN** for the new term.

**Body — `UpdateLessonDto`:**

| Field           | Type               |
|-----------------|--------------------|
| `courseId`      | int                |
| `instructorId`  | guid               |
| `roomId`        | int                |
| `academicYear`  | int                |
| `semester`      | `AcademicSemester` |
| `maxCapacity`   | int                |

---

### `DELETE /api/admin/lessons/{lessonId}`

Deletes a lesson.

---

### `GET /api/admin/lessons/{lessonId}/enrollments`

Returns current enrollments for a lesson, enriched from the synced auth-user directory.

**Response:** `EnrollmentDto[]`

| Field             | Type   |
|-------------------|--------|
| `id`              | int    |
| `lessonId`        | int    |
| `studentId`       | guid   |
| `studentFullName` | string |
| `studentCode`     | string |
| `studentEmail`    | string |

---

### `POST /api/admin/lessons/{lessonId}/enrollments`

Enrolls one real auth-service student into a lesson.

**Body — `CreateEnrollmentDto`:**

| Field       | Type | Notes |
|-------------|------|-------|
| `studentId` | guid | Required id; validated in local directory/id-only mode |

**Response:** `201 Created`, body `EnrollmentDto`

---

## Instructors (admin)

### `GET /api/admin/instructors/{instructorId}/lessons`

Returns lessons assigned to the given instructor.

**Response:** `LessonDto[]`

---

## Sessions and attendance (admin)

### `GET /api/admin/lessons/{lessonId}/sessions`

All class sessions for a lesson, with attendance rollups.

**Response:** `SessionDto[]`

**`SessionDto` (high level):** `id`, `lessonId`, `lessonName` (course title), `startTime`, `endTime`, `topic`, `isAttendanceActive`, activation timestamps, `totalStudents`, `presentCount`, `lateCount`, `absentCount`, `excusedCount`

---

### `POST /api/admin/lessons/{lessonId}/sessions/generate`

Bulk-creates `LessonSession` rows between **`fromDate`** and **`toDate`** (inclusive) for each calendar day that matches a **weekly slot** (`dayOfWeek` + wall-clock `startTime` / `endTime`). Sessions that already exist with the same **date + start + end** are skipped.

**Limits:** date span ≤ 731 days; total proposed sessions ≤ 2000 per request.

**Body — `BulkGenerateSessionsDto`:**

| Field          | Type                      | Notes |
|----------------|---------------------------|--------|
| `fromDate`     | `DateOnly` (JSON date)    | Inclusive |
| `toDate`       | `DateOnly`                | Inclusive, must be ≥ `fromDate` |
| `weeklySlots`  | `WeeklySessionSlotDto[]`  | At least one slot |
| `topic`        | string?                   | Optional; applied to every created session |

**`WeeklySessionSlotDto`:**

| Field         | Type        | Notes |
|---------------|-------------|--------|
| `dayOfWeek`   | `DayOfWeek` | JSON string enum, e.g. `Monday`, `Tuesday` (server calendar; `Sunday` = 0 … `Saturday` = 6 in .NET) |
| `startTime`   | `TimeOnly`  | e.g. `"09:00:00"` |
| `endTime`     | `TimeOnly`  | Must be after `startTime` |

**Response — `BulkGenerateSessionsResponseDto`:**

| Field                     | Type          |
|---------------------------|---------------|
| `createdCount`            | int           |
| `skippedDuplicateCount`   | int           |
| `createdSessions`         | `SessionDto[]` | New rows only, ordered by date/time |

**Instructor mirror:** `POST /api/instructors/{instructorId}/lessons/{lessonId}/sessions/generate` (same body; `instructorId` must match the authenticated user; caller must own the lesson).

---

### `GET /api/admin/sessions/{sessionId}/attendance`

Full attendance roster for a session.

**Response:** `AttendanceDto[]`

**`AttendanceDto` (high level):** `id`, `sessionId`, `lessonId`, `studentId`, `studentFullName`, `studentCode`, `status`, marking/scan fields, `isManuallyAdjusted`, `instructorNote`, etc.

---

### `PATCH /api/admin/sessions/{sessionId}/attendance/{attendanceId}`

Admin correction for one attendance row (`attendanceId`). `sessionId` is part of the route for REST consistency.

**Body — `AdminAttendanceCorrectionDto`:**

| Field    | Type    | Notes                                                |
|----------|---------|------------------------------------------------------|
| `status` | string  | One of: `Present`, `Late`, `Absent`, `Excused`       |
| `note`   | string? | Optional                                             |

---

### `DELETE /api/admin/sessions/{sessionId}/attendance/{attendanceId}`

Deletes an attendance record (admin).

---

## Shared DTOs and enums

### `CourseDto`

| Field            | Type   |
|------------------|--------|
| `id`             | int    |
| `name`           | string |
| `department`     | string |
| `code`           | string |
| `credits`        | int    |
| `timesPerWeek`   | int    |

### `LessonDto`

| Field           | Type               | Notes |
|-----------------|--------------------|-------|
| `id`            | int                |       |
| `name`          | string             | **Course catalog title** (not stored separately on the lesson) |
| `code`          | string             | Course code |
| `instructorId`  | guid               | Auth-service instructor id |
| `instructorDisplayName` | string     | Synced auth user display name |
| `instructorEmail` | string           | Synced auth user email |
| `academicYear`  | int                |       |
| `semester`      | `AcademicSemester` |       |
| `crn`           | string             | Five characters; see CRN rules below (property `CRN` in C#) |
| `maxCapacity`   | int                |       |
| `sessions`      | `SessionShortDto[]`? | Present on detail reads when returned by the service |

### `SessionShortDto` (nested on lesson detail)

| Field                 | Type    |
|-----------------------|---------|
| `id`                  | int     |
| `date`                | date    |
| `startTime` / `endTime` | time  |
| `topic`               | string? |
| `isAttendanceActive`  | bool    |

### `AcademicSemester` (enum)

Used in lesson DTOs and for CRN generation (first digit).

| JSON (typical) | Integer | CRN first digit |
|----------------|---------|-----------------|
| `Fall`         | `1`     | `1`             |
| `Spring`       | `2`     | `2`             |
| `Summer`       | `3`     | `3`             |

### `AttendanceStatus` (for `AttendanceDto.status` / corrections)

String values: **`Present`**, **`Late`**, **`Absent`**, **`Excused`**.

---

## CRN rules (reference)

- **Format:** 5 characters: **one** digit (`1` / `2` / `3` matching semester) + **four** digits (`0001`–`9999`).
- **Scope:** Sequence resets per **`(academicYear, semester)`**.
- **Uniqueness:** **`(academicYear, Semester, CRN)`** is unique in the database.
- **Client rule:** Omit `crn` on create; it is assigned by the server.

---

## Document version

Aligned with `AdminController` and application DTOs in this repository. Regenerate or edit this file when routes or contracts change.
