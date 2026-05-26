# Classroom Training Management — Implementation Plan

> **For Antigravity:** REQUIRED WORKFLOW: Use `.agent/workflows/execute-plan.md` to execute this plan in single-flow mode.

**Goal:** Implement a classroom training management module supporting physical, virtual, and hybrid sessions with instructor management, resource booking, attendance tracking, and a template/batch system.

**Architecture:** New bounded context within the monolith. All entities extend `BaseEntity`, implement `ITenantEntity`, and follow existing CQRS patterns (MediatR + GenericRepository). EF Core configurations use `IEntityTypeConfiguration<T>`. PostgreSQL with snake_case naming.

**Tech Stack:** .NET 9, EF Core, MediatR, FluentValidation, PostgreSQL, xUnit

**Design doc:** `docs/plans/2026-05-26-classroom-training-design.md`

---

### Task 1: Domain Enums

**Files:**
- Create: `src/backend/JLT.Domain/Enums/SessionMode.cs`
- Create: `src/backend/JLT.Domain/Enums/MeetingProvider.cs`
- Create: `src/backend/JLT.Domain/Enums/ResourceType.cs`
- Create: `src/backend/JLT.Domain/Enums/BatchStatus.cs`
- Create: `src/backend/JLT.Domain/Enums/SessionStatus.cs`
- Create: `src/backend/JLT.Domain/Enums/EnrollmentStatus.cs`
- Create: `src/backend/JLT.Domain/Enums/AttendanceStatus.cs`
- Create: `src/backend/JLT.Domain/Enums/AttendanceSource.cs`

**Step 1: Create all enum files**

```csharp
// SessionMode.cs
namespace JLT.Domain.Enums;
public enum SessionMode { Physical, Virtual, Hybrid }

// MeetingProvider.cs
namespace JLT.Domain.Enums;
public enum MeetingProvider { None, Zoom, Teams }

// ResourceType.cs
namespace JLT.Domain.Enums;
public enum ResourceType { Room, Lab, Equipment }

// BatchStatus.cs
namespace JLT.Domain.Enums;
public enum BatchStatus { Scheduled, InProgress, Completed, Cancelled }

// SessionStatus.cs
namespace JLT.Domain.Enums;
public enum SessionStatus { Scheduled, InProgress, Completed, Cancelled }

// EnrollmentStatus.cs
namespace JLT.Domain.Enums;
public enum EnrollmentStatus { Enrolled, Waitlisted, Completed, Withdrawn }

// AttendanceStatus.cs
namespace JLT.Domain.Enums;
public enum AttendanceStatus { Present, Absent, Late, Excused }

// AttendanceSource.cs
namespace JLT.Domain.Enums;
public enum AttendanceSource { Manual, QRCode, VirtualAutomatic }
```

**Step 2: Verify build**

Run: `dotnet build src/backend/JLT.Domain/JLT.Domain.csproj`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add src/backend/JLT.Domain/Enums/
git commit -m "feat(domain): add classroom training enums"
```

---

### Task 2: Core Domain Entities — PhysicalResource & InstructorProfile

**Files:**
- Create: `src/backend/JLT.Domain/Entities/PhysicalResource.cs`
- Create: `src/backend/JLT.Domain/Entities/InstructorProfile.cs`
- Test: `src/backend/JLT.Tests/Domain/Classrooms/ResourceAndInstructorTests.cs`

**Step 1: Write the failing test**

```csharp
// ResourceAndInstructorTests.cs
using Xunit;
using JLT.Domain.Entities;
using JLT.Domain.Enums;

namespace JLT.Tests.Domain.Classrooms;

public class ResourceAndInstructorTests
{
    [Fact]
    public void PhysicalResource_DefaultsToActive()
    {
        var resource = new PhysicalResource
        {
            Name = "Room A101",
            Type = ResourceType.Room,
            Location = "Building A, Floor 1",
            Capacity = 30
        };
        Assert.True(resource.IsActive);
        Assert.Equal("Room A101", resource.Name);
        Assert.Equal(ResourceType.Room, resource.Type);
    }

    [Fact]
    public void InstructorProfile_CanBeCreatedWithUserId()
    {
        var userId = Guid.NewGuid();
        var profile = new InstructorProfile
        {
            UserId = userId,
            Bio = "Senior .NET instructor",
            IsActive = true
        };
        Assert.Equal(userId, profile.UserId);
        Assert.True(profile.IsActive);
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test src/backend/JLT.Tests/JLT.Tests.csproj --filter "ResourceAndInstructorTests" --no-restore`
Expected: FAIL — `PhysicalResource` and `InstructorProfile` types not found

**Step 3: Write minimal implementation**

```csharp
// PhysicalResource.cs
using JLT.Domain.Common;
using JLT.Domain.Enums;

namespace JLT.Domain.Entities;

public class PhysicalResource : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ResourceType Type { get; set; }
    public string? Location { get; set; }
    public int? Capacity { get; set; }
    public bool IsActive { get; set; } = true;
}
```

```csharp
// InstructorProfile.cs
using JLT.Domain.Common;

namespace JLT.Domain.Entities;

public class InstructorProfile : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string? Bio { get; set; }
    public string? Specializations { get; set; } // jsonb
    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual User? User { get; set; }
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test src/backend/JLT.Tests/JLT.Tests.csproj --filter "ResourceAndInstructorTests"`
Expected: PASS (2 tests)

**Step 5: Commit**

```bash
git add .
git commit -m "feat(domain): add PhysicalResource and InstructorProfile entities"
```

---

### Task 3: Core Domain Entities — TrainingTemplate & TemplateSession

**Files:**
- Create: `src/backend/JLT.Domain/Entities/TrainingTemplate.cs`
- Create: `src/backend/JLT.Domain/Entities/TemplateSession.cs`
- Test: `src/backend/JLT.Tests/Domain/Classrooms/TemplateTests.cs`

**Step 1: Write the failing test**

```csharp
// TemplateTests.cs
using Xunit;
using JLT.Domain.Entities;
using JLT.Domain.Enums;

namespace JLT.Tests.Domain.Classrooms;

public class TemplateTests
{
    [Fact]
    public void TrainingTemplate_CanAddTemplateSessions()
    {
        var template = new TrainingTemplate
        {
            Name = "Onboarding Program",
            Description = "3-day onboarding",
            Category = "HR"
        };

        template.Sessions.Add(new TemplateSession
        {
            Title = "Day 1: Company Overview",
            SortOrder = 1,
            DurationMinutes = 120,
            SessionMode = SessionMode.Physical
        });

        template.Sessions.Add(new TemplateSession
        {
            Title = "Day 2: Tools & Systems",
            SortOrder = 2,
            DurationMinutes = 180,
            SessionMode = SessionMode.Virtual
        });

        Assert.Equal(2, template.Sessions.Count);
        Assert.True(template.IsActive);
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test src/backend/JLT.Tests/JLT.Tests.csproj --filter "TemplateTests" --no-restore`
Expected: FAIL

**Step 3: Write minimal implementation**

```csharp
// TrainingTemplate.cs
using JLT.Domain.Common;

namespace JLT.Domain.Entities;

public class TrainingTemplate : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual ICollection<TemplateSession> Sessions { get; set; } = new List<TemplateSession>();
}
```

```csharp
// TemplateSession.cs
using JLT.Domain.Common;
using JLT.Domain.Enums;

namespace JLT.Domain.Entities;

public class TemplateSession : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid TrainingTemplateId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public int DurationMinutes { get; set; }
    public SessionMode SessionMode { get; set; }

    // Navigation
    public virtual TrainingTemplate? TrainingTemplate { get; set; }
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test src/backend/JLT.Tests/JLT.Tests.csproj --filter "TemplateTests"`
Expected: PASS

**Step 5: Commit**

```bash
git add .
git commit -m "feat(domain): add TrainingTemplate and TemplateSession entities"
```

---

### Task 4: Core Domain Entities — TrainingBatch, Session, SessionInstructor

**Files:**
- Create: `src/backend/JLT.Domain/Entities/TrainingBatch.cs`
- Create: `src/backend/JLT.Domain/Entities/Session.cs`
- Create: `src/backend/JLT.Domain/Entities/SessionInstructor.cs`
- Test: `src/backend/JLT.Tests/Domain/Classrooms/BatchAndSessionTests.cs`

**Step 1: Write the failing test**

```csharp
// BatchAndSessionTests.cs
using Xunit;
using JLT.Domain.Entities;
using JLT.Domain.Enums;

namespace JLT.Tests.Domain.Classrooms;

public class BatchAndSessionTests
{
    [Fact]
    public void TrainingBatch_DefaultsToScheduled()
    {
        var batch = new TrainingBatch
        {
            Name = "July 2026 Cohort",
            StartDate = new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc)
        };
        Assert.Equal(BatchStatus.Scheduled, batch.Status);
    }

    [Fact]
    public void Session_CanAssignInstructorAndResource()
    {
        var instructorId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();

        var session = new Session
        {
            Title = "Day 1: Intro",
            SessionMode = SessionMode.Hybrid,
            StartTime = new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc),
            EndTime = new DateTime(2026, 7, 1, 11, 0, 0, DateTimeKind.Utc),
            PhysicalResourceId = resourceId
        };

        session.SessionInstructors.Add(new SessionInstructor
        {
            InstructorProfileId = instructorId
        });

        Assert.Equal(SessionStatus.Scheduled, session.Status);
        Assert.Equal(SessionMode.Hybrid, session.SessionMode);
        Assert.Single(session.SessionInstructors);
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test src/backend/JLT.Tests/JLT.Tests.csproj --filter "BatchAndSessionTests" --no-restore`
Expected: FAIL

**Step 3: Write minimal implementation**

```csharp
// TrainingBatch.cs
using JLT.Domain.Common;
using JLT.Domain.Enums;

namespace JLT.Domain.Entities;

public class TrainingBatch : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid TrainingTemplateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public BatchStatus Status { get; set; } = BatchStatus.Scheduled;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? MaxParticipants { get; set; }

    // Navigation
    public virtual TrainingTemplate? TrainingTemplate { get; set; }
    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();
    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
```

```csharp
// Session.cs
using JLT.Domain.Common;
using JLT.Domain.Enums;

namespace JLT.Domain.Entities;

public class Session : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid TrainingBatchId { get; set; }
    public Guid? TemplateSessionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public SessionMode SessionMode { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public MeetingProvider? MeetingProvider { get; set; }
    public string? ExternalMeetingId { get; set; }
    public string? JoinUrl { get; set; }
    public Guid? PhysicalResourceId { get; set; }
    public SessionStatus Status { get; set; } = SessionStatus.Scheduled;

    // Navigation
    public virtual TrainingBatch? TrainingBatch { get; set; }
    public virtual PhysicalResource? PhysicalResource { get; set; }
    public virtual ICollection<SessionInstructor> SessionInstructors { get; set; } = new List<SessionInstructor>();
    public virtual ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
}
```

```csharp
// SessionInstructor.cs
using JLT.Domain.Common;

namespace JLT.Domain.Entities;

public class SessionInstructor : BaseEntity
{
    public Guid SessionId { get; set; }
    public Guid InstructorProfileId { get; set; }

    // Navigation
    public virtual Session? Session { get; set; }
    public virtual InstructorProfile? InstructorProfile { get; set; }
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test src/backend/JLT.Tests/JLT.Tests.csproj --filter "BatchAndSessionTests"`
Expected: PASS (2 tests)

**Step 5: Commit**

```bash
git add .
git commit -m "feat(domain): add TrainingBatch, Session, and SessionInstructor entities"
```

---

### Task 5: Core Domain Entities — Enrollment & AttendanceRecord

**Files:**
- Create: `src/backend/JLT.Domain/Entities/Enrollment.cs`
- Create: `src/backend/JLT.Domain/Entities/AttendanceRecord.cs`
- Test: `src/backend/JLT.Tests/Domain/Classrooms/EnrollmentAndAttendanceTests.cs`

**Step 1: Write the failing test**

```csharp
// EnrollmentAndAttendanceTests.cs
using Xunit;
using JLT.Domain.Entities;
using JLT.Domain.Enums;

namespace JLT.Tests.Domain.Classrooms;

public class EnrollmentAndAttendanceTests
{
    [Fact]
    public void Enrollment_DefaultsToEnrolledStatus()
    {
        var enrollment = new Enrollment
        {
            UserId = Guid.NewGuid(),
            TrainingBatchId = Guid.NewGuid(),
            EnrolledAt = DateTime.UtcNow
        };
        Assert.Equal(EnrollmentStatus.Enrolled, enrollment.Status);
    }

    [Fact]
    public void AttendanceRecord_CanTrackVirtualAutomatic()
    {
        var record = new AttendanceRecord
        {
            SessionId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Status = AttendanceStatus.Present,
            Source = AttendanceSource.VirtualAutomatic,
            CheckInTime = DateTime.UtcNow
        };
        Assert.Equal(AttendanceSource.VirtualAutomatic, record.Source);
        Assert.Equal(AttendanceStatus.Present, record.Status);
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test src/backend/JLT.Tests/JLT.Tests.csproj --filter "EnrollmentAndAttendanceTests" --no-restore`
Expected: FAIL

**Step 3: Write minimal implementation**

```csharp
// Enrollment.cs
using JLT.Domain.Common;
using JLT.Domain.Enums;

namespace JLT.Domain.Entities;

public class Enrollment : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid TrainingBatchId { get; set; }
    public Guid UserId { get; set; }
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Enrolled;
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual TrainingBatch? TrainingBatch { get; set; }
    public virtual User? User { get; set; }
}
```

```csharp
// AttendanceRecord.cs
using JLT.Domain.Common;
using JLT.Domain.Enums;

namespace JLT.Domain.Entities;

public class AttendanceRecord : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid SessionId { get; set; }
    public Guid UserId { get; set; }
    public AttendanceStatus Status { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public AttendanceSource Source { get; set; } = AttendanceSource.Manual;

    // Navigation
    public virtual Session? Session { get; set; }
    public virtual User? User { get; set; }
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test src/backend/JLT.Tests/JLT.Tests.csproj --filter "EnrollmentAndAttendanceTests"`
Expected: PASS (2 tests)

**Step 5: Commit**

```bash
git add .
git commit -m "feat(domain): add Enrollment and AttendanceRecord entities"
```

---

### Task 6: Domain Interface — IVirtualMeetingProvider

**Files:**
- Create: `src/backend/JLT.Domain/Interfaces/IVirtualMeetingProvider.cs`

**Step 1: Create the interface**

```csharp
// IVirtualMeetingProvider.cs
namespace JLT.Domain.Interfaces;

public record VirtualMeetingResult(string ExternalMeetingId, string JoinUrl);

public interface IVirtualMeetingProvider
{
    Task<VirtualMeetingResult> ScheduleMeetingAsync(string title, DateTime startTime, DateTime endTime, CancellationToken ct = default);
    Task CancelMeetingAsync(string externalMeetingId, CancellationToken ct = default);
}
```

**Step 2: Verify build**

Run: `dotnet build src/backend/JLT.Domain/JLT.Domain.csproj`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add .
git commit -m "feat(domain): add IVirtualMeetingProvider interface"
```

---

### Task 7: EF Core Configurations

**Files:**
- Create: `src/backend/JLT.Infrastructure/Persistence/Configurations/ClassroomConfiguration.cs`
- Modify: `src/backend/JLT.Infrastructure/Persistence/AppDbContext.cs` — add DbSets and query filters

**Step 1: Create ClassroomConfiguration.cs**

All entity configurations for the classroom module in a single file (matching the existing pattern of grouping related configs, e.g. `GroupAndAuditConfiguration.cs`).

```csharp
// ClassroomConfiguration.cs
using JLT.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JLT.Infrastructure.Persistence.Configurations;

public class PhysicalResourceConfiguration : IEntityTypeConfiguration<PhysicalResource>
{
    public void Configure(EntityTypeBuilder<PhysicalResource> builder)
    {
        builder.ToTable("physical_resources");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Type).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.Location).HasMaxLength(500);
        builder.HasIndex(e => e.TenantId);
    }
}

public class InstructorProfileConfiguration : IEntityTypeConfiguration<InstructorProfile>
{
    public void Configure(EntityTypeBuilder<InstructorProfile> builder)
    {
        builder.ToTable("instructor_profiles");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Bio).HasColumnType("text");
        builder.Property(e => e.Specializations).HasColumnType("jsonb");
        builder.HasIndex(e => new { e.TenantId, e.UserId }).IsUnique();
        builder.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class TrainingTemplateConfiguration : IEntityTypeConfiguration<TrainingTemplate>
{
    public void Configure(EntityTypeBuilder<TrainingTemplate> builder)
    {
        builder.ToTable("training_templates");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(500);
        builder.Property(e => e.Description).HasColumnType("text");
        builder.Property(e => e.Category).HasMaxLength(200);
        builder.HasIndex(e => e.TenantId);
        builder.HasMany(e => e.Sessions).WithOne(s => s.TrainingTemplate).HasForeignKey(s => s.TrainingTemplateId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class TemplateSessionConfiguration : IEntityTypeConfiguration<TemplateSession>
{
    public void Configure(EntityTypeBuilder<TemplateSession> builder)
    {
        builder.ToTable("template_sessions");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Title).IsRequired().HasMaxLength(500);
        builder.Property(e => e.Description).HasColumnType("text");
        builder.Property(e => e.SessionMode).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(e => new { e.TrainingTemplateId, e.SortOrder });
    }
}

public class TrainingBatchConfiguration : IEntityTypeConfiguration<TrainingBatch>
{
    public void Configure(EntityTypeBuilder<TrainingBatch> builder)
    {
        builder.ToTable("training_batches");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(500);
        builder.Property(e => e.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.TenantId, e.Status });
        builder.HasOne(e => e.TrainingTemplate).WithMany().HasForeignKey(e => e.TrainingTemplateId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(e => e.Sessions).WithOne(s => s.TrainingBatch).HasForeignKey(s => s.TrainingBatchId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Enrollments).WithOne(en => en.TrainingBatch).HasForeignKey(en => en.TrainingBatchId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("sessions");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Title).IsRequired().HasMaxLength(500);
        builder.Property(e => e.Description).HasColumnType("text");
        builder.Property(e => e.SessionMode).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.MeetingProvider).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.ExternalMeetingId).HasMaxLength(200);
        builder.Property(e => e.JoinUrl).HasMaxLength(2048);
        builder.Property(e => e.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.TenantId, e.StartTime, e.EndTime });
        builder.HasIndex(e => new { e.PhysicalResourceId, e.StartTime, e.EndTime });
        builder.HasOne(e => e.PhysicalResource).WithMany().HasForeignKey(e => e.PhysicalResourceId).OnDelete(DeleteBehavior.SetNull);
        builder.HasMany(e => e.SessionInstructors).WithOne(si => si.Session).HasForeignKey(si => si.SessionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.AttendanceRecords).WithOne(a => a.Session).HasForeignKey(a => a.SessionId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class SessionInstructorConfiguration : IEntityTypeConfiguration<SessionInstructor>
{
    public void Configure(EntityTypeBuilder<SessionInstructor> builder)
    {
        builder.ToTable("session_instructors");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.SessionId, e.InstructorProfileId }).IsUnique();
        builder.HasOne(e => e.InstructorProfile).WithMany().HasForeignKey(e => e.InstructorProfileId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.ToTable("enrollments");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(e => new { e.TrainingBatchId, e.UserId }).IsUnique();
        builder.HasIndex(e => e.TenantId);
        builder.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.ToTable("attendance_records");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.Source).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(e => new { e.SessionId, e.UserId }).IsUnique();
        builder.HasIndex(e => e.TenantId);
        builder.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
```

**Step 2: Add DbSets and query filters to AppDbContext.cs**

Add after the existing `// Learning Content Management` section (around line 41):

```csharp
// Classroom Training Management
public DbSet<PhysicalResource> PhysicalResources => Set<PhysicalResource>();
public DbSet<InstructorProfile> InstructorProfiles => Set<InstructorProfile>();
public DbSet<TrainingTemplate> TrainingTemplates => Set<TrainingTemplate>();
public DbSet<TemplateSession> TemplateSessions => Set<TemplateSession>();
public DbSet<TrainingBatch> TrainingBatches => Set<TrainingBatch>();
public DbSet<Session> Sessions => Set<Session>();
public DbSet<SessionInstructor> SessionInstructors => Set<SessionInstructor>();
public DbSet<Enrollment> Enrollments => Set<Enrollment>();
public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
```

Add query filters in `OnModelCreating` after the existing LCM filters (around line 59):

```csharp
// Classroom Training tenant isolation
modelBuilder.Entity<PhysicalResource>().HasQueryFilter(e => !_tenantContext.IsResolved || e.TenantId == _tenantContext.TenantId);
modelBuilder.Entity<InstructorProfile>().HasQueryFilter(e => !_tenantContext.IsResolved || e.TenantId == _tenantContext.TenantId);
modelBuilder.Entity<TrainingTemplate>().HasQueryFilter(e => !_tenantContext.IsResolved || e.TenantId == _tenantContext.TenantId);
modelBuilder.Entity<TemplateSession>().HasQueryFilter(e => !_tenantContext.IsResolved || e.TenantId == _tenantContext.TenantId);
modelBuilder.Entity<TrainingBatch>().HasQueryFilter(e => !_tenantContext.IsResolved || e.TenantId == _tenantContext.TenantId);
modelBuilder.Entity<Session>().HasQueryFilter(e => !_tenantContext.IsResolved || e.TenantId == _tenantContext.TenantId);
modelBuilder.Entity<Enrollment>().HasQueryFilter(e => !_tenantContext.IsResolved || e.TenantId == _tenantContext.TenantId);
modelBuilder.Entity<AttendanceRecord>().HasQueryFilter(e => !_tenantContext.IsResolved || e.TenantId == _tenantContext.TenantId);
```

**Step 3: Verify build**

Run: `dotnet build src/backend/JLT.sln`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add .
git commit -m "feat(infra): add EF configurations, DbSets, and query filters for classroom module"
```

---

### Task 8: EF Migration

**Step 1: Generate migration**

Run: `dotnet ef migrations add AddClassroomTraining --project src/backend/JLT.Infrastructure/JLT.Infrastructure.csproj --startup-project src/backend/JLT.API/JLT.API.csproj`
Expected: Migration file created

**Step 2: Apply migration**

Run: `dotnet ef database update --project src/backend/JLT.Infrastructure/JLT.Infrastructure.csproj --startup-project src/backend/JLT.API/JLT.API.csproj`
Expected: Database updated

**Step 3: Commit**

```bash
git add .
git commit -m "feat(infra): add AddClassroomTraining migration"
```

---

### Task 9: Application Layer — TrainingTemplate CQRS Commands

**Files:**
- Create: `src/backend/JLT.Application/DTOs/ClassroomDtos.cs`
- Create: `src/backend/JLT.Application/Features/Classrooms/TrainingTemplateCommands.cs`

**Step 1: Create DTOs**

```csharp
// ClassroomDtos.cs
namespace JLT.Application.DTOs;

public record TrainingTemplateDto(
    Guid Id, Guid TenantId, string Name, string? Description, string? Category,
    bool IsActive, DateTime CreatedAt, DateTime UpdatedAt,
    List<TemplateSessionDto> Sessions);

public record TemplateSessionDto(
    Guid Id, string Title, string? Description, int SortOrder,
    int DurationMinutes, string SessionMode);

public record TrainingBatchDto(
    Guid Id, Guid TenantId, Guid TrainingTemplateId, string Name, string Status,
    DateTime StartDate, DateTime? EndDate, int? MaxParticipants,
    DateTime CreatedAt, DateTime UpdatedAt);

public record SessionDto(
    Guid Id, Guid TrainingBatchId, string Title, string? Description, string SessionMode,
    DateTime StartTime, DateTime EndTime, string? MeetingProvider,
    string? JoinUrl, Guid? PhysicalResourceId, string Status,
    DateTime CreatedAt, DateTime UpdatedAt);

public record EnrollmentDto(
    Guid Id, Guid TrainingBatchId, Guid UserId, string Status, DateTime EnrolledAt);

public record AttendanceRecordDto(
    Guid Id, Guid SessionId, Guid UserId, string Status, string Source,
    DateTime? CheckInTime, DateTime? CheckOutTime);

public record PhysicalResourceDto(
    Guid Id, Guid TenantId, string Name, string Type, string? Location,
    int? Capacity, bool IsActive);

public record InstructorProfileDto(
    Guid Id, Guid TenantId, Guid UserId, string? Bio,
    string? Specializations, bool IsActive);
```

**Step 2: Create TrainingTemplateCommands.cs**

```csharp
// TrainingTemplateCommands.cs
using FluentValidation;
using JLT.Application.DTOs;
using JLT.Domain.Entities;
using JLT.Domain.Enums;
using JLT.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JLT.Application.Features.Classrooms;

// Mapper
internal static class ClassroomMapper
{
    public static TrainingTemplateDto ToDto(TrainingTemplate t) => new(
        t.Id, t.TenantId, t.Name, t.Description, t.Category,
        t.IsActive, t.CreatedAt, t.UpdatedAt,
        t.Sessions.OrderBy(s => s.SortOrder).Select(s => new TemplateSessionDto(
            s.Id, s.Title, s.Description, s.SortOrder, s.DurationMinutes, s.SessionMode.ToString()
        )).ToList());
}

// Get by ID
public record GetTrainingTemplateByIdQuery(Guid Id) : IRequest<TrainingTemplateDto?>;

public class GetTrainingTemplateByIdHandler : IRequestHandler<GetTrainingTemplateByIdQuery, TrainingTemplateDto?>
{
    private readonly IRepository<TrainingTemplate> _repo;
    public GetTrainingTemplateByIdHandler(IRepository<TrainingTemplate> repo) => _repo = repo;

    public async Task<TrainingTemplateDto?> Handle(GetTrainingTemplateByIdQuery request, CancellationToken ct)
    {
        var template = await _repo.Query()
            .Include(t => t.Sessions)
            .FirstOrDefaultAsync(t => t.Id == request.Id, ct);
        return template == null ? null : ClassroomMapper.ToDto(template);
    }
}

// Create
public record CreateTrainingTemplateCommand(
    string Name, string? Description, string? Category,
    List<CreateTemplateSessionInput>? Sessions) : IRequest<TrainingTemplateDto>;

public record CreateTemplateSessionInput(
    string Title, string? Description, int SortOrder, int DurationMinutes, string SessionMode);

public class CreateTrainingTemplateValidator : AbstractValidator<CreateTrainingTemplateCommand>
{
    public CreateTrainingTemplateValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(500);
    }
}

public class CreateTrainingTemplateHandler : IRequestHandler<CreateTrainingTemplateCommand, TrainingTemplateDto>
{
    private readonly IRepository<TrainingTemplate> _repo;
    public CreateTrainingTemplateHandler(IRepository<TrainingTemplate> repo) => _repo = repo;

    public async Task<TrainingTemplateDto> Handle(CreateTrainingTemplateCommand request, CancellationToken ct)
    {
        var template = new TrainingTemplate
        {
            Name = request.Name,
            Description = request.Description,
            Category = request.Category
        };

        if (request.Sessions != null)
        {
            foreach (var s in request.Sessions)
            {
                template.Sessions.Add(new TemplateSession
                {
                    Title = s.Title,
                    Description = s.Description,
                    SortOrder = s.SortOrder,
                    DurationMinutes = s.DurationMinutes,
                    SessionMode = Enum.Parse<SessionMode>(s.SessionMode)
                });
            }
        }

        await _repo.AddAsync(template, ct);
        return ClassroomMapper.ToDto(template);
    }
}
```

**Step 3: Verify build**

Run: `dotnet build src/backend/JLT.sln`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add .
git commit -m "feat(app): add classroom DTOs and TrainingTemplate CQRS commands"
```

---

### Task 10: Application Layer — TrainingBatch CQRS Commands (with batch generation)

**Files:**
- Create: `src/backend/JLT.Application/Features/Classrooms/TrainingBatchCommands.cs`

**Step 1: Create TrainingBatchCommands.cs**

```csharp
// TrainingBatchCommands.cs
using FluentValidation;
using JLT.Application.DTOs;
using JLT.Domain.Entities;
using JLT.Domain.Enums;
using JLT.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JLT.Application.Features.Classrooms;

// Create Batch from Template (generates sessions)
public record CreateTrainingBatchCommand(
    Guid TrainingTemplateId, string Name, DateTime StartDate,
    int? MaxParticipants) : IRequest<TrainingBatchDto>;

public class CreateTrainingBatchValidator : AbstractValidator<CreateTrainingBatchCommand>
{
    public CreateTrainingBatchValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TrainingTemplateId).NotEmpty();
        RuleFor(x => x.StartDate).GreaterThan(DateTime.MinValue);
    }
}

public class CreateTrainingBatchHandler : IRequestHandler<CreateTrainingBatchCommand, TrainingBatchDto>
{
    private readonly IRepository<TrainingBatch> _batchRepo;
    private readonly IRepository<TrainingTemplate> _templateRepo;

    public CreateTrainingBatchHandler(IRepository<TrainingBatch> batchRepo, IRepository<TrainingTemplate> templateRepo)
    {
        _batchRepo = batchRepo;
        _templateRepo = templateRepo;
    }

    public async Task<TrainingBatchDto> Handle(CreateTrainingBatchCommand request, CancellationToken ct)
    {
        var template = await _templateRepo.Query()
            .Include(t => t.Sessions)
            .FirstOrDefaultAsync(t => t.Id == request.TrainingTemplateId, ct)
            ?? throw new KeyNotFoundException("Training template not found.");

        var batch = new TrainingBatch
        {
            TrainingTemplateId = template.Id,
            Name = request.Name,
            StartDate = request.StartDate,
            MaxParticipants = request.MaxParticipants
        };

        // Generate sessions from template
        var currentStart = request.StartDate;
        foreach (var ts in template.Sessions.OrderBy(s => s.SortOrder))
        {
            batch.Sessions.Add(new Session
            {
                TemplateSessionId = ts.Id,
                Title = ts.Title,
                Description = ts.Description,
                SessionMode = ts.SessionMode,
                StartTime = currentStart,
                EndTime = currentStart.AddMinutes(ts.DurationMinutes)
            });
            // Next session starts after this one (same day, sequential)
            currentStart = currentStart.AddMinutes(ts.DurationMinutes + 15); // 15 min break
        }

        batch.EndDate = batch.Sessions.Max(s => s.EndTime);
        await _batchRepo.AddAsync(batch, ct);

        return new TrainingBatchDto(
            batch.Id, batch.TenantId, batch.TrainingTemplateId, batch.Name,
            batch.Status.ToString(), batch.StartDate, batch.EndDate,
            batch.MaxParticipants, batch.CreatedAt, batch.UpdatedAt);
    }
}
```

**Step 2: Verify build**

Run: `dotnet build src/backend/JLT.sln`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add .
git commit -m "feat(app): add TrainingBatch CQRS commands with batch generation engine"
```

---

### Task 11: API Controllers

**Files:**
- Create: `src/backend/JLT.API/Controllers/TrainingTemplatesController.cs`
- Create: `src/backend/JLT.API/Controllers/TrainingBatchesController.cs`

**Step 1: Create TrainingTemplatesController.cs**

```csharp
using JLT.Application.Common;
using JLT.Application.DTOs;
using JLT.Application.Features.Classrooms;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JLT.API.Controllers;

[ApiController]
[Route("api/training-templates")]
[Authorize]
public class TrainingTemplatesController : ControllerBase
{
    private readonly IMediator _mediator;
    public TrainingTemplatesController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetTrainingTemplateByIdQuery(id));
        if (result == null) return NotFound(ApiResponse.Fail("Template not found."));
        return Ok(ApiResponse<TrainingTemplateDto>.Ok(result));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTrainingTemplateCommand command)
    {
        var result = await _mediator.Send(command);
        return Created($"/api/training-templates/{result.Id}", ApiResponse<TrainingTemplateDto>.Ok(result, "Template created."));
    }
}
```

**Step 2: Create TrainingBatchesController.cs**

```csharp
using JLT.Application.Common;
using JLT.Application.DTOs;
using JLT.Application.Features.Classrooms;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JLT.API.Controllers;

[ApiController]
[Route("api/training-batches")]
[Authorize]
public class TrainingBatchesController : ControllerBase
{
    private readonly IMediator _mediator;
    public TrainingBatchesController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTrainingBatchCommand command)
    {
        var result = await _mediator.Send(command);
        return Created($"/api/training-batches/{result.Id}", ApiResponse<TrainingBatchDto>.Ok(result, "Batch created."));
    }
}
```

**Step 3: Verify build**

Run: `dotnet build src/backend/JLT.sln`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add .
git commit -m "feat(api): add TrainingTemplates and TrainingBatches controllers"
```

---

### Task 12: Run Full Test Suite

**Step 1: Start the API backend**

Run: `dotnet run --project src/backend/JLT.API/JLT.API.csproj` (background)
Wait for: "Now listening on: http://localhost:5126"

**Step 2: Run all tests**

Run: `dotnet test src/backend/JLT.Tests/JLT.Tests.csproj`
Expected: All tests pass (existing + new domain tests)

**Step 3: Commit (if any fixes were needed)**

```bash
git add .
git commit -m "fix: resolve any test failures"
```
