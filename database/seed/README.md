# Database seed (lessons)

`lessons.pipe.txt` is a **pipe-delimited** export of your course list (102 sections).

## Automatic seed (recommended)

The Web app copies `lessons.pipe.txt` to `SeedData/` on build. On startup, **after** EF migrations run, if `Database:SeedLessonsIfEmpty` is **true** and **`Lessons` has no rows**, the app inserts all 102 lessons.

- **Docker Compose**: `Database__SeedLessonsIfEmpty=true` is set on the backend service.
- **Local Development**: enabled in `appsettings.Development.json`.
- **Production**: keep `Database:SeedLessonsIfEmpty` **false** in `appsettings.json` unless you intentionally want a one-time fill on an empty database.

## Manual script (truncate + insert)

`Populate-Database.ps1` truncates lesson-related tables and inserts one row per section into `"Lessons"`. Use this if you need to **reset** data or run outside the app.

## Column mapping

| Source column        | `Lessons` column | Notes |
|---------------------|------------------|--------|
| courseTitle         | `Name`           | |
| subjectDescription  | `Department`     | |
| courseNumber        | `Code`           | |
| CRN                 | `CRN`            | |
| instructor          | _(derived)_      | Primary name before `(Primary)` → `InstructorId` (1…N, stable sort) |
| availableSeats      | `Capacity`       | |
| lessonsPWeek        | `TimesPerWeek`   | |
| _(parameter)_       | `Semester`       | Default `Spring2026` |
| _(parameter)_       | `RoomId`         | Default `1` |
| _(parameter)_       | `Credits`        | Default `3` (not in CSV) |
| _(fixed)_           | `Type`           | `Section` |

`InstructorId` values are **synthetic** (first instructor → 1, …). They are not joined to a users table unless you add one and remap.

## Run (local PostgreSQL)

Requires `psql` on `PATH`.

```powershell
cd database\seed
$env:PGPASSWORD = '12345'
.\Populate-Database.ps1 -HostName localhost -Port 5432 -Database my_ada_attendance_db -UserName postgres
```

## Run (Docker Compose)

Generate SQL only, then pipe into the DB container (service name from your `docker-compose.yml`, often `db`):

```powershell
cd database\seed
.\Populate-Database.ps1 -SqlOnly -OutputSqlPath .\seed.sql
Get-Content .\seed.sql | docker compose exec -T db psql -U postgres -d my_ada_attendance_db
```

## Extra tables

If your database has newer migrations (e.g. `"AttendanceScanLogs"`, `"AttendanceActivations"`) with foreign keys to sessions, add them to the `TRUNCATE` list in `Populate-Database.ps1` **before** `"LessonSessions"` / `"SessionAttendances"` as required by your schema.
