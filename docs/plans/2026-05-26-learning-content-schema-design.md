# Learning Content Management — Schema Design

**Date**: 2026-05-26
**Scope**: Entity schema for the Learning Content Management (LCM) module — content repository, tracking, and LRS domains.
**Constraint**: Content only. Course/lesson structures belong to the Course Management module.

---

## Content Types

| ContentType | MimeType Examples | Storage | Tracking |
|-------------|-------------------|---------|----------|
| `Document` | `application/pdf`, `text/plain`, `text/html` (rich text) | StorageUrl | ✅ Progress + Bookmark (page) |
| `Media` | `video/mp4`, `video/quicktime`, `audio/wav`, `audio/mpeg` | StorageUrl | ✅ Progress + Bookmark (timeSeconds) |
| `SCORM` | `application/zip` (manifest) | StorageUrl | ✅ CMI state (ScormRuntimeState) |
| `xAPI` | Varies | StorageUrl or ExternalUrl | ✅ xAPI statements (xApiStatement) |
| `LTI` | N/A (external tool) | ExternalUrl + Config (credentials) | ✅ Progress + Bookmark (section) |
| `Hyperlink` | N/A | ExternalUrl | ❌ Display only |
| `EmbedLink` | N/A | ExternalUrl + Config (dimensions) | ❌ Display only |
| `Image` | `image/png`, `image/jpeg`, `image/webp` | StorageUrl | ❌ Display only |

---

## LearningContent Entity

### Identity

| Attribute | Type | Nullable | Notes |
|-----------|------|----------|-------|
| `Id` | Guid | No | PK |
| `TenantId` | Guid | No | FK → tenants |
| `Title` | string | No | Max 500 chars |
| `Description` | string | Yes | Long text |
| `ContentType` | Enum | No | Document, Media, SCORM, xAPI, LTI, Hyperlink, EmbedLink, Image |
| `Status` | Enum | No | Draft, InReview, Published, Archived, Expired |
| `Version` | string | No | Semantic version, e.g. "1.0" |

### Storage

| Attribute | Type | Nullable | Notes |
|-----------|------|----------|-------|
| `MimeType` | string | Yes | IANA media type |
| `StorageUrl` | string | Yes | S3/CDN/local path for uploaded files |
| `FileSize` | long | Yes | Bytes |
| `DurationSeconds` | int | Yes | For media files |
| `ExternalUrl` | string | Yes | For Hyperlink, EmbedLink, LTI |
| `Config` | JSONB | Yes | Type-specific metadata: LTI credentials, SCORM manifest path, xAPI activity IRI, rich-text body, embed dimensions |
| `ThumbnailUrl` | string | Yes | Preview image |

### Lifecycle & Governance

| Attribute | Type | Nullable | Notes |
|-----------|------|----------|-------|
| `CreatedAt` | DateTime | No | Auto-set on insert |
| `UpdatedAt` | DateTime | No | Auto-set on update |
| `CreatedBy` | Guid | No | FK → users |
| `UpdatedBy` | Guid | No | FK → users |
| `PublishedAt` | DateTime | Yes | Set when status moves to Published |
| `ValidFrom` | DateTime | Yes | Content becomes available after this date |
| `ValidTill` | DateTime | Yes | Content auto-expires after this date |
| `RetiredAt` | DateTime | Yes | When manually archived/retired |
| `NextReviewDate` | DateTime | Yes | Periodic review schedule |
| `ReviewedBy` | Guid | Yes | FK → users, who last reviewed |
| `ReviewedAt` | DateTime | Yes | When last reviewed/approved |

### Source & Provenance

| Attribute | Type | Nullable | Notes |
|-----------|------|----------|-------|
| `ContentSource` | Enum | No | Internal, External, Partner, AIGenerated |
| `SourceUrl` | string | Yes | Original URL if sourced externally |
| `Author` | string | Yes | Content author name (may differ from CreatedBy) |
| `Publisher` | string | Yes | Publishing organization/vendor |
| `Copyright` | string | Yes | Copyright notice text |
| `LicenseType` | string | Yes | Free-text: CC-BY-4.0, Proprietary, All Rights Reserved, etc. |

### Discovery & Organization

| Attribute | Type | Nullable | Notes |
|-----------|------|----------|-------|
| `Language` | string | No | ISO 639-1 code (en, fr, ar, etc.) |
| `Locale` | string | Yes | Regional variant (en-US, en-GB, ar-SA) |
| `EstimatedDurationMinutes` | int | Yes | Expected consumption time |
| `Category` | string | Yes | Broad topic: Compliance, Onboarding, Technical |
| `Tags` | JSONB | Yes | Unified searchable tags/keywords array |

---

## Content Lifecycle State Machine

```
Draft → InReview → Published → Archived
                                  ↓
                               Expired

- Draft: Initial creation, editable.
- InReview: Submitted for review, locked for editing.
- Published: Live and available (subject to ValidFrom/ValidTill).
- Archived: Manually retired by admin.
- Expired: Auto-transitioned when ValidTill passes.
```

---

## Tracking & LRS Domain

### ContentProgress (Unified Tracker)

Tracks per-user progress for Document, Media, LTI, and generic trackable content.

| Attribute | Type | Nullable | Notes |
|-----------|------|----------|-------|
| `Id` | Guid | No | PK |
| `UserId` | Guid | No | FK → users |
| `LearningContentId` | Guid | No | FK → learning_content |
| `Status` | Enum | No | NotStarted, InProgress, Completed |
| `ProgressPercent` | decimal | No | 0.0 to 100.0 |
| `BookmarkData` | JSONB | Yes | Type-specific resume: `{"page": 12}`, `{"timeSeconds": 342}`, `{"section": "module-3"}` |
| `TimeSpentSeconds` | int | No | Cumulative time |
| `CompletedAt` | DateTime | Yes | |
| `LastAccessedAt` | DateTime | No | |

### ScormPackage

| Attribute | Type | Nullable | Notes |
|-----------|------|----------|-------|
| `Id` | Guid | No | PK |
| `LearningContentId` | Guid | No | FK → learning_content |
| `EntryPoint` | string | No | e.g. index.html |
| `ScormVersion` | string | No | SCORM_1.2 or SCORM_2004 |
| `ManifestData` | JSONB | No | Parsed imsmanifest.xml |

### ScormRuntimeState

| Attribute | Type | Nullable | Notes |
|-----------|------|----------|-------|
| `Id` | Guid | No | PK |
| `UserId` | Guid | No | FK → users |
| `ScormPackageId` | Guid | No | FK → scorm_packages |
| `LessonStatus` | string | No | completed, incomplete, passed, failed, not attempted |
| `LessonLocation` | string | Yes | Bookmark |
| `SuspendData` | string | Yes | Opaque state blob |
| `RawScore` | decimal | Yes | |
| `MinScore` | decimal | Yes | |
| `MaxScore` | decimal | Yes | |
| `SessionTime` | string | Yes | |
| `TotalTime` | string | Yes | |
| `Entry` | string | Yes | ab-initio or resume |

### xApiStatement

| Attribute | Type | Nullable | Notes |
|-----------|------|----------|-------|
| `Id` | Guid | No | PK |
| `TenantId` | Guid | No | FK → tenants |
| `ActorJson` | JSONB | No | Agent/group identifier |
| `VerbId` | string | No | IRI |
| `ObjectJson` | JSONB | No | Activity/statement ref |
| `ResultJson` | JSONB | Yes | Score, success, completion, duration |
| `ContextJson` | JSONB | Yes | Registration, extensions |
| `Timestamp` | DateTime | No | |
| `StoredAt` | DateTime | No | |

---

## ContentTag (Lookup Entity)

| Attribute | Type | Nullable | Notes |
|-----------|------|----------|-------|
| `Id` | Guid | No | PK |
| `TenantId` | Guid | No | FK → tenants |
| `Name` | string | No | Unique per tenant |

---

## Design Decisions

1. **Single LearningContent table** with `ContentType` enum — polymorphism via `Config` JSONB for type-specific metadata instead of separate tables per type.
2. **ContentProgress** is the unified tracker for Document, Media, and LTI. `BookmarkData` JSONB stores type-specific resume points.
3. **SCORM and xAPI** get dedicated tables — their data models are fundamentally different (CMI state vs. immutable statements).
4. **Hyperlink, EmbedLink, Image** have no tracking — display-only content assets.
5. **Tags** is a single JSONB field merging keywords and tags for simplicity.
6. **LicenseType** is free-text to support arbitrary license standards without enum maintenance.
7. **PublishedAt** is distinct from CreatedAt — captures the exact moment content went live.
8. **ValidFrom/ValidTill** enable scheduled publishing and automatic expiration.
9. **Review fields** (NextReviewDate, ReviewedBy, ReviewedAt) support periodic content governance workflows.
