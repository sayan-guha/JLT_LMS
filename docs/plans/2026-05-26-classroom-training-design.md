# Classroom Training Management Module — Design

## Goal

Add a classroom training management module to the JLT LMS that supports scheduling, delivering, and tracking instructor-led trainings in physical, virtual (Zoom/Teams), and hybrid modalities. Includes instructor management, physical resource management, attendance tracking, and a template/batch system for rapid course deployment.

## Architecture

The module lives as a new bounded context within the existing monolith. All entities are tenant-scoped (`ITenantEntity`), extend `BaseEntity`, and follow the existing CQRS pattern (MediatR commands/queries → repository → PostgreSQL via EF Core).

## Entities

### TrainingTemplate

Blueprint for a training course. No dates, instructors, or venues — only syllabus structure.

| Field | Type | Notes |
|-------|------|-------|
| Id | Guid | PK, from BaseEntity |
| TenantId | Guid | ITenantEntity |
| Name | string | Required, max 500 |
| Description | string? | text |
| Category | string? | max 200 |
| IsActive | bool | default true |
| Sessions | nav → TemplateSession[] | ordered session blueprints |

### TemplateSession

Defines a session blueprint within a template (order and duration only).

| Field | Type | Notes |
|-------|------|-------|
| Id | Guid | PK |
| TenantId | Guid | ITenantEntity |
| TrainingTemplateId | Guid | FK |
| Title | string | max 500 |
| Description | string? | text |
| SortOrder | int | ordering within template |
| DurationMinutes | int | expected duration |
| SessionMode | enum | Physical / Virtual / Hybrid |

### TrainingBatch

An instantiation of a TrainingTemplate. Represents a specific cohort or run.

| Field | Type | Notes |
|-------|------|-------|
| Id | Guid | PK |
| TenantId | Guid | ITenantEntity |
| TrainingTemplateId | Guid | FK |
| Name | string | max 500, e.g. "Batch 3 — July 2026" |
| Status | enum | Scheduled / InProgress / Completed / Cancelled |
| StartDate | DateTime | first session date |
| EndDate | DateTime? | last session date |
| MaxParticipants | int? | capacity cap |
| Sessions | nav → Session[] | concrete sessions |
| Enrollments | nav → Enrollment[] | |

### Session

A concrete, scheduled occurrence. The core unit of delivery.

| Field | Type | Notes |
|-------|------|-------|
| Id | Guid | PK |
| TenantId | Guid | ITenantEntity |
| TrainingBatchId | Guid | FK |
| TemplateSessionId | Guid? | FK back to blueprint |
| Title | string | max 500 |
| Description | string? | text |
| SessionMode | enum | Physical / Virtual / Hybrid |
| StartTime | DateTime | UTC |
| EndTime | DateTime | UTC |
| MeetingProvider | enum? | Zoom / Teams / None |
| ExternalMeetingId | string? | from provider API |
| JoinUrl | string? | max 2048 |
| PhysicalResourceId | Guid? | FK |
| Status | enum | Scheduled / InProgress / Completed / Cancelled |
| SessionInstructors | nav → SessionInstructor[] | |
| AttendanceRecords | nav → AttendanceRecord[] | |

### PhysicalResource

Rooms, labs, projectors, or any bookable equipment.

| Field | Type | Notes |
|-------|------|-------|
| Id | Guid | PK |
| TenantId | Guid | ITenantEntity |
| Name | string | max 200 |
| Type | enum | Room / Lab / Equipment |
| Location | string? | max 500 |
| Capacity | int? | |
| IsActive | bool | default true |

### InstructorProfile

Extends User with instructor-specific metadata.

| Field | Type | Notes |
|-------|------|-------|
| Id | Guid | PK |
| TenantId | Guid | ITenantEntity |
| UserId | Guid | FK → User, unique |
| Bio | string? | text |
| Specializations | string? | jsonb |
| IsActive | bool | default true |

### SessionInstructor

Junction: assigns instructor(s) to a session.

| Field | Type | Notes |
|-------|------|-------|
| Id | Guid | PK |
| SessionId | Guid | FK |
| InstructorProfileId | Guid | FK |

### Enrollment

Tracks who is registered for a batch.

| Field | Type | Notes |
|-------|------|-------|
| Id | Guid | PK |
| TenantId | Guid | ITenantEntity |
| TrainingBatchId | Guid | FK |
| UserId | Guid | FK → User |
| Status | enum | Enrolled / Waitlisted / Completed / Withdrawn |
| EnrolledAt | DateTime | UTC |

### AttendanceRecord

Per-session, per-user attendance.

| Field | Type | Notes |
|-------|------|-------|
| Id | Guid | PK |
| TenantId | Guid | ITenantEntity |
| SessionId | Guid | FK |
| UserId | Guid | FK → User |
| Status | enum | Present / Absent / Late / Excused |
| CheckInTime | DateTime? | |
| CheckOutTime | DateTime? | |
| Source | enum | Manual / QRCode / VirtualAutomatic |

## Enums

- **SessionMode**: Physical, Virtual, Hybrid
- **MeetingProvider**: None, Zoom, Teams
- **ResourceType**: Room, Lab, Equipment
- **BatchStatus**: Scheduled, InProgress, Completed, Cancelled
- **SessionStatus**: Scheduled, InProgress, Completed, Cancelled
- **EnrollmentStatus**: Enrolled, Waitlisted, Completed, Withdrawn
- **AttendanceStatus**: Present, Absent, Late, Excused
- **AttendanceSource**: Manual, QRCode, VirtualAutomatic

## Interfaces

- **IVirtualMeetingProvider**: `ScheduleMeetingAsync`, `CancelMeetingAsync`, `GetJoinUrlAsync`
- **ITrainingRepository**: Standard CRUD + `GetBatchWithSessionsAsync`, `GetSessionsForInstructorAsync`
- **IResourceBookingService**: `IsResourceAvailableAsync(resourceId, start, end)`, `IsInstructorAvailableAsync(instructorId, start, end)`

## Conflict Resolution

- Before saving a Session: check `PhysicalResource` availability (no overlapping sessions for same resource).
- Before assigning an instructor: check instructor availability (no overlapping sessions for same instructor).
- Both checks are application-layer validations in the command handlers.

## Virtual Meeting Integration

- On Session create/update (when Mode is Virtual/Hybrid): call `IVirtualMeetingProvider.ScheduleMeetingAsync` → store `ExternalMeetingId` + `JoinUrl`.
- Webhook endpoint `POST /api/webhooks/meetings` receives join/leave events → creates `AttendanceRecord` with Source=VirtualAutomatic.

## Template → Batch Flow

1. Admin creates a `TrainingTemplate` with N `TemplateSessions`.
2. Admin creates a `TrainingBatch` from the template, providing a start date.
3. System generates N `Session` records (dates calculated from start date + session durations/order).
4. Admin assigns instructors, venues, and meeting providers to each session.
