# Learning Content Management — Implementation Plan

> **For Antigravity:** REQUIRED WORKFLOW: Use `.agent/workflows/execute-plan.md` to execute this plan in single-flow mode.

**Goal:** Build the Learning Content Management (LCM) module — entities, database schema, repositories, CQRS handlers, and REST API for content CRUD, lifecycle management, progress tracking, SCORM runtime, and xAPI statement storage.

**Architecture:** Clean Architecture layers (Domain → Infrastructure → Application → API). Follows the same patterns as the existing User Management module: `BaseEntity`/`ITenantEntity` base types, `GenericRepository<T>` + specialized repos, MediatR command/query handlers with FluentValidation, and `ApiResponse<T>` controller wrappers. All content entities are tenant-scoped with global query filters.

**Tech Stack:** .NET 9, EF Core + PostgreSQL (JSONB, GIN indexes), MediatR, FluentValidation, xUnit + Moq

---

## Task 1: Domain Enums

**Files:**
- Create: `src/backend/JLT.Domain/Enums/ContentType.cs`
- Create: `src/backend/JLT.Domain/Enums/ContentStatus.cs`
- Create: `src/backend/JLT.Domain/Enums/ContentSource.cs`
- Create: `src/backend/JLT.Domain/Enums/ProgressStatus.cs`

**Step 1: Create ContentType enum**

```csharp
// src/backend/JLT.Domain/Enums/ContentType.cs
namespace JLT.Domain.Enums;

public enum ContentType
{
    Document,
    Media,
    SCORM,
    xAPI,
    LTI,
    Hyperlink,
    EmbedLink,
    Image
}
```

**Step 2: Create ContentStatus enum**

```csharp
// src/backend/JLT.Domain/Enums/ContentStatus.cs
namespace JLT.Domain.Enums;

public enum ContentStatus
{
    Draft,
    InReview,
    Published,
    Archived,
    Expired
}
```

**Step 3: Create ContentSource enum**

```csharp
// src/backend/JLT.Domain/Enums/ContentSource.cs
namespace JLT.Domain.Enums;

public enum ContentSource
{
    Internal,
    External,
    Partner,
    AIGenerated
}
```

**Step 4: Create ProgressStatus enum**

```csharp
// src/backend/JLT.Domain/Enums/ProgressStatus.cs
namespace JLT.Domain.Enums;

public enum ProgressStatus
{
    NotStarted,
    InProgress,
    Completed
}
```

**Step 5: Verify build**

Run: `dotnet build src/backend/JLT.Domain/JLT.Domain.csproj`
Expected: Build succeeded.

**Step 6: Commit**

```bash
git add src/backend/JLT.Domain/Enums/ContentType.cs src/backend/JLT.Domain/Enums/ContentStatus.cs src/backend/JLT.Domain/Enums/ContentSource.cs src/backend/JLT.Domain/Enums/ProgressStatus.cs
git commit -m "feat(lcm): add domain enums for content type, status, source, and progress"
```

---

## Task 2: Domain Entities — LearningContent & ContentTag

**Files:**
- Create: `src/backend/JLT.Domain/Entities/LearningContent.cs`
- Create: `src/backend/JLT.Domain/Entities/ContentTag.cs`

**Step 1: Create LearningContent entity**

```csharp
// src/backend/JLT.Domain/Entities/LearningContent.cs
using JLT.Domain.Common;
using JLT.Domain.Enums;

namespace JLT.Domain.Entities;

public class LearningContent : BaseEntity, ITenantEntity
{
    // Identity
    public Guid TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ContentType ContentType { get; set; }
    public ContentStatus Status { get; set; } = ContentStatus.Draft;
    public string Version { get; set; } = "1.0";

    // Storage
    public string? MimeType { get; set; }
    public string? StorageUrl { get; set; }
    public long? FileSize { get; set; }
    public int? DurationSeconds { get; set; }
    public string? ExternalUrl { get; set; }
    public string? Config { get; set; }           // JSONB
    public string? ThumbnailUrl { get; set; }

    // Lifecycle & Governance
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTill { get; set; }
    public DateTime? RetiredAt { get; set; }
    public DateTime? NextReviewDate { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }

    // Source & Provenance
    public ContentSource ContentSource { get; set; } = ContentSource.Internal;
    public string? SourceUrl { get; set; }
    public string? Author { get; set; }
    public string? Publisher { get; set; }
    public string? Copyright { get; set; }
    public string? LicenseType { get; set; }

    // Discovery & Organization
    public string Language { get; set; } = "en";
    public string? Locale { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public string? Category { get; set; }
    public string? Tags { get; set; }              // JSONB

    // Navigation
    public virtual ScormPackage? ScormPackage { get; set; }
    public virtual ICollection<ContentProgress> ProgressRecords { get; set; } = new List<ContentProgress>();
}
```

**Step 2: Create ContentTag entity**

```csharp
// src/backend/JLT.Domain/Entities/ContentTag.cs
using JLT.Domain.Common;

namespace JLT.Domain.Entities;

public class ContentTag : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
}
```

**Step 3: Verify build**

Run: `dotnet build src/backend/JLT.Domain/JLT.Domain.csproj`
Expected: Build succeeded.

**Step 4: Commit**

```bash
git add src/backend/JLT.Domain/Entities/LearningContent.cs src/backend/JLT.Domain/Entities/ContentTag.cs
git commit -m "feat(lcm): add LearningContent and ContentTag domain entities"
```

---

## Task 3: Domain Entities — Tracking & LRS

**Files:**
- Create: `src/backend/JLT.Domain/Entities/ContentProgress.cs`
- Create: `src/backend/JLT.Domain/Entities/ScormPackage.cs`
- Create: `src/backend/JLT.Domain/Entities/ScormRuntimeState.cs`
- Create: `src/backend/JLT.Domain/Entities/XApiStatement.cs`

**Step 1: Create ContentProgress entity**

```csharp
// src/backend/JLT.Domain/Entities/ContentProgress.cs
using JLT.Domain.Common;
using JLT.Domain.Enums;

namespace JLT.Domain.Entities;

public class ContentProgress : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid LearningContentId { get; set; }
    public ProgressStatus Status { get; set; } = ProgressStatus.NotStarted;
    public decimal ProgressPercent { get; set; }
    public string? BookmarkData { get; set; }       // JSONB
    public int TimeSpentSeconds { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual LearningContent? LearningContent { get; set; }
}
```

**Step 2: Create ScormPackage entity**

```csharp
// src/backend/JLT.Domain/Entities/ScormPackage.cs
using JLT.Domain.Common;

namespace JLT.Domain.Entities;

public class ScormPackage : BaseEntity
{
    public Guid LearningContentId { get; set; }
    public string EntryPoint { get; set; } = string.Empty;
    public string ScormVersion { get; set; } = string.Empty;  // SCORM_1.2 or SCORM_2004
    public string ManifestData { get; set; } = "{}";           // JSONB

    // Navigation
    public virtual LearningContent? LearningContent { get; set; }
    public virtual ICollection<ScormRuntimeState> RuntimeStates { get; set; } = new List<ScormRuntimeState>();
}
```

**Step 3: Create ScormRuntimeState entity**

```csharp
// src/backend/JLT.Domain/Entities/ScormRuntimeState.cs
using JLT.Domain.Common;

namespace JLT.Domain.Entities;

public class ScormRuntimeState : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid ScormPackageId { get; set; }
    public string LessonStatus { get; set; } = "not attempted";
    public string? LessonLocation { get; set; }
    public string? SuspendData { get; set; }
    public decimal? RawScore { get; set; }
    public decimal? MinScore { get; set; }
    public decimal? MaxScore { get; set; }
    public string? SessionTime { get; set; }
    public string? TotalTime { get; set; }
    public string? Entry { get; set; }

    // Navigation
    public virtual ScormPackage? ScormPackage { get; set; }
}
```

**Step 4: Create XApiStatement entity**

```csharp
// src/backend/JLT.Domain/Entities/XApiStatement.cs
using JLT.Domain.Common;

namespace JLT.Domain.Entities;

public class XApiStatement : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string ActorJson { get; set; } = "{}";       // JSONB
    public string VerbId { get; set; } = string.Empty;
    public string ObjectJson { get; set; } = "{}";       // JSONB
    public string? ResultJson { get; set; }               // JSONB
    public string? ContextJson { get; set; }              // JSONB
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public DateTime StoredAt { get; set; } = DateTime.UtcNow;
}
```

**Step 5: Verify build**

Run: `dotnet build src/backend/JLT.Domain/JLT.Domain.csproj`
Expected: Build succeeded.

**Step 6: Commit**

```bash
git add src/backend/JLT.Domain/Entities/ContentProgress.cs src/backend/JLT.Domain/Entities/ScormPackage.cs src/backend/JLT.Domain/Entities/ScormRuntimeState.cs src/backend/JLT.Domain/Entities/XApiStatement.cs
git commit -m "feat(lcm): add tracking entities — ContentProgress, ScormPackage, ScormRuntimeState, XApiStatement"
```

---

## Task 4: Domain Interfaces — LCM Repositories

**Files:**
- Create: `src/backend/JLT.Domain/Interfaces/ILearningContentRepository.cs`
- Create: `src/backend/JLT.Domain/Interfaces/IContentProgressRepository.cs`
- Create: `src/backend/JLT.Domain/Interfaces/IScormRepository.cs`
- Create: `src/backend/JLT.Domain/Interfaces/IXApiStatementRepository.cs`

**Step 1: Create ILearningContentRepository**

```csharp
// src/backend/JLT.Domain/Interfaces/ILearningContentRepository.cs
using JLT.Domain.Entities;
using JLT.Domain.Enums;

namespace JLT.Domain.Interfaces;

public interface ILearningContentRepository : IRepository<LearningContent>
{
    Task<IReadOnlyList<LearningContent>> GetByContentTypeAsync(ContentType type, CancellationToken ct = default);
    Task<IReadOnlyList<LearningContent>> GetByStatusAsync(ContentStatus status, CancellationToken ct = default);
    Task<IReadOnlyList<LearningContent>> GetExpiredContentAsync(DateTime now, CancellationToken ct = default);
}
```

**Step 2: Create IContentProgressRepository**

```csharp
// src/backend/JLT.Domain/Interfaces/IContentProgressRepository.cs
using JLT.Domain.Entities;

namespace JLT.Domain.Interfaces;

public interface IContentProgressRepository : IRepository<ContentProgress>
{
    Task<ContentProgress?> GetByUserAndContentAsync(Guid userId, Guid contentId, CancellationToken ct = default);
    Task<IReadOnlyList<ContentProgress>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<ContentProgress>> GetByContentAsync(Guid contentId, CancellationToken ct = default);
}
```

**Step 3: Create IScormRepository**

```csharp
// src/backend/JLT.Domain/Interfaces/IScormRepository.cs
using JLT.Domain.Entities;

namespace JLT.Domain.Interfaces;

public interface IScormRepository
{
    // ScormPackage
    Task<ScormPackage?> GetPackageByContentIdAsync(Guid learningContentId, CancellationToken ct = default);
    Task<ScormPackage> AddPackageAsync(ScormPackage package, CancellationToken ct = default);

    // ScormRuntimeState
    Task<ScormRuntimeState?> GetRuntimeStateAsync(Guid userId, Guid scormPackageId, CancellationToken ct = default);
    Task<ScormRuntimeState> UpsertRuntimeStateAsync(ScormRuntimeState state, CancellationToken ct = default);
}
```

**Step 4: Create IXApiStatementRepository**

```csharp
// src/backend/JLT.Domain/Interfaces/IXApiStatementRepository.cs
using JLT.Domain.Entities;

namespace JLT.Domain.Interfaces;

public interface IXApiStatementRepository : IRepository<XApiStatement>
{
    Task<IReadOnlyList<XApiStatement>> GetByVerbAsync(string verbId, CancellationToken ct = default);
    Task<IReadOnlyList<XApiStatement>> GetByActorAsync(string actorJson, CancellationToken ct = default);
}
```

**Step 5: Verify build**

Run: `dotnet build src/backend/JLT.Domain/JLT.Domain.csproj`
Expected: Build succeeded.

**Step 6: Commit**

```bash
git add src/backend/JLT.Domain/Interfaces/ILearningContentRepository.cs src/backend/JLT.Domain/Interfaces/IContentProgressRepository.cs src/backend/JLT.Domain/Interfaces/IScormRepository.cs src/backend/JLT.Domain/Interfaces/IXApiStatementRepository.cs
git commit -m "feat(lcm): add repository interfaces for content, progress, SCORM, and xAPI"
```

---

## Task 5: EF Core Configurations

**Files:**
- Create: `src/backend/JLT.Infrastructure/Persistence/Configurations/LearningContentConfiguration.cs`
- Create: `src/backend/JLT.Infrastructure/Persistence/Configurations/TrackingConfiguration.cs`

**Step 1: Create LearningContentConfiguration**

This file contains configurations for `LearningContent` and `ContentTag`. Follow the pattern in `UserConfiguration.cs` — snake_case table names, JSONB columns, GIN indexes.

```csharp
// src/backend/JLT.Infrastructure/Persistence/Configurations/LearningContentConfiguration.cs
using JLT.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JLT.Infrastructure.Persistence.Configurations;

public class LearningContentConfiguration : IEntityTypeConfiguration<LearningContent>
{
    public void Configure(EntityTypeBuilder<LearningContent> builder)
    {
        builder.ToTable("learning_content");
        builder.HasKey(c => c.Id);

        // Identity
        builder.Property(c => c.Title).IsRequired().HasMaxLength(500);
        builder.Property(c => c.Description).HasColumnType("text");
        builder.Property(c => c.ContentType).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.Version).IsRequired().HasMaxLength(20);

        // Storage
        builder.Property(c => c.MimeType).HasMaxLength(100);
        builder.Property(c => c.StorageUrl).HasMaxLength(2048);
        builder.Property(c => c.ExternalUrl).HasMaxLength(2048);
        builder.Property(c => c.Config).HasColumnType("jsonb");
        builder.Property(c => c.ThumbnailUrl).HasMaxLength(2048);

        // Lifecycle
        builder.Property(c => c.CreatedBy).IsRequired();
        builder.Property(c => c.UpdatedBy).IsRequired();

        // Provenance
        builder.Property(c => c.ContentSource).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.SourceUrl).HasMaxLength(2048);
        builder.Property(c => c.Author).HasMaxLength(200);
        builder.Property(c => c.Publisher).HasMaxLength(200);
        builder.Property(c => c.Copyright).HasMaxLength(500);
        builder.Property(c => c.LicenseType).HasMaxLength(100);

        // Discovery
        builder.Property(c => c.Language).IsRequired().HasMaxLength(10);
        builder.Property(c => c.Locale).HasMaxLength(10);
        builder.Property(c => c.Category).HasMaxLength(200);
        builder.Property(c => c.Tags).HasColumnType("jsonb");

        // Indexes
        builder.HasIndex(c => c.TenantId);
        builder.HasIndex(c => new { c.TenantId, c.Status });
        builder.HasIndex(c => new { c.TenantId, c.ContentType });
        builder.HasIndex(c => new { c.TenantId, c.Category });
        builder.HasIndex(c => c.Tags).HasMethod("gin").HasOperators("jsonb_path_ops");

        // Navigation: ScormPackage (1:1)
        builder.HasOne(c => c.ScormPackage)
            .WithOne(s => s.LearningContent)
            .HasForeignKey<ScormPackage>(s => s.LearningContentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Navigation: ContentProgress (1:many)
        builder.HasMany(c => c.ProgressRecords)
            .WithOne(p => p.LearningContent)
            .HasForeignKey(p => p.LearningContentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ContentTagConfiguration : IEntityTypeConfiguration<ContentTag>
{
    public void Configure(EntityTypeBuilder<ContentTag> builder)
    {
        builder.ToTable("content_tags");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(t => new { t.TenantId, t.Name }).IsUnique();
        builder.HasIndex(t => t.TenantId);
    }
}
```

**Step 2: Create TrackingConfiguration**

This file contains EF configurations for `ContentProgress`, `ScormPackage`, `ScormRuntimeState`, and `XApiStatement`.

```csharp
// src/backend/JLT.Infrastructure/Persistence/Configurations/TrackingConfiguration.cs
using JLT.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JLT.Infrastructure.Persistence.Configurations;

public class ContentProgressConfiguration : IEntityTypeConfiguration<ContentProgress>
{
    public void Configure(EntityTypeBuilder<ContentProgress> builder)
    {
        builder.ToTable("content_progress");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.ProgressPercent).HasPrecision(5, 2);
        builder.Property(p => p.BookmarkData).HasColumnType("jsonb");

        builder.HasIndex(p => new { p.UserId, p.LearningContentId }).IsUnique();
        builder.HasIndex(p => p.UserId);
        builder.HasIndex(p => p.LearningContentId);
    }
}

public class ScormPackageConfiguration : IEntityTypeConfiguration<ScormPackage>
{
    public void Configure(EntityTypeBuilder<ScormPackage> builder)
    {
        builder.ToTable("scorm_packages");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.EntryPoint).IsRequired().HasMaxLength(500);
        builder.Property(s => s.ScormVersion).IsRequired().HasMaxLength(20);
        builder.Property(s => s.ManifestData).IsRequired().HasColumnType("jsonb");

        builder.HasIndex(s => s.LearningContentId).IsUnique();

        builder.HasMany(s => s.RuntimeStates)
            .WithOne(r => r.ScormPackage)
            .HasForeignKey(r => r.ScormPackageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ScormRuntimeStateConfiguration : IEntityTypeConfiguration<ScormRuntimeState>
{
    public void Configure(EntityTypeBuilder<ScormRuntimeState> builder)
    {
        builder.ToTable("scorm_runtime_states");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.LessonStatus).IsRequired().HasMaxLength(50);
        builder.Property(r => r.LessonLocation).HasMaxLength(1000);
        builder.Property(r => r.SuspendData).HasColumnType("text");
        builder.Property(r => r.RawScore).HasPrecision(10, 2);
        builder.Property(r => r.MinScore).HasPrecision(10, 2);
        builder.Property(r => r.MaxScore).HasPrecision(10, 2);
        builder.Property(r => r.SessionTime).HasMaxLength(50);
        builder.Property(r => r.TotalTime).HasMaxLength(50);
        builder.Property(r => r.Entry).HasMaxLength(20);

        builder.HasIndex(r => new { r.UserId, r.ScormPackageId }).IsUnique();
        builder.HasIndex(r => r.UserId);
    }
}

public class XApiStatementConfiguration : IEntityTypeConfiguration<XApiStatement>
{
    public void Configure(EntityTypeBuilder<XApiStatement> builder)
    {
        builder.ToTable("xapi_statements");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ActorJson).IsRequired().HasColumnType("jsonb");
        builder.Property(x => x.VerbId).IsRequired().HasMaxLength(500);
        builder.Property(x => x.ObjectJson).IsRequired().HasColumnType("jsonb");
        builder.Property(x => x.ResultJson).HasColumnType("jsonb");
        builder.Property(x => x.ContextJson).HasColumnType("jsonb");

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.VerbId });
        builder.HasIndex(x => new { x.TenantId, x.Timestamp });
        builder.HasIndex(x => x.ActorJson).HasMethod("gin").HasOperators("jsonb_path_ops");
    }
}
```

**Step 3: Verify build**

Run: `dotnet build src/backend/JLT.Infrastructure/JLT.Infrastructure.csproj`
Expected: Build succeeded.

**Step 4: Commit**

```bash
git add src/backend/JLT.Infrastructure/Persistence/Configurations/LearningContentConfiguration.cs src/backend/JLT.Infrastructure/Persistence/Configurations/TrackingConfiguration.cs
git commit -m "feat(lcm): add EF Core configurations for all LCM entities"
```

---

## Task 6: Register DbSets & Query Filters in AppDbContext

**Files:**
- Modify: `src/backend/JLT.Infrastructure/Persistence/AppDbContext.cs`

**Step 1: Add DbSet properties**

Add these DbSet declarations after the existing `AuditLogs` line (line 33):

```csharp
    // Learning Content Management
    public DbSet<LearningContent> LearningContent => Set<LearningContent>();
    public DbSet<ContentTag> ContentTags => Set<ContentTag>();
    public DbSet<ContentProgress> ContentProgress => Set<ContentProgress>();
    public DbSet<ScormPackage> ScormPackages => Set<ScormPackage>();
    public DbSet<ScormRuntimeState> ScormRuntimeStates => Set<ScormRuntimeState>();
    public DbSet<XApiStatement> XApiStatements => Set<XApiStatement>();
```

**Step 2: Add query filters in OnModelCreating**

Add these after the existing `HasQueryFilter` lines (after line 46):

```csharp
        modelBuilder.Entity<LearningContent>().HasQueryFilter(e => !_tenantContext.IsResolved || e.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<ContentTag>().HasQueryFilter(e => !_tenantContext.IsResolved || e.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<XApiStatement>().HasQueryFilter(e => !_tenantContext.IsResolved || e.TenantId == _tenantContext.TenantId);
```

**Step 3: Add `using JLT.Domain.Enums;`** at top if not already present.

**Step 4: Verify build**

Run: `dotnet build src/backend/JLT.Infrastructure/JLT.Infrastructure.csproj`
Expected: Build succeeded.

**Step 5: Commit**

```bash
git add src/backend/JLT.Infrastructure/Persistence/AppDbContext.cs
git commit -m "feat(lcm): register LCM DbSets and tenant query filters in AppDbContext"
```

---

## Task 7: Repository Implementations

**Files:**
- Create: `src/backend/JLT.Infrastructure/Repositories/LearningContentRepository.cs`
- Create: `src/backend/JLT.Infrastructure/Repositories/ContentProgressRepository.cs`
- Create: `src/backend/JLT.Infrastructure/Repositories/ScormRepository.cs`
- Create: `src/backend/JLT.Infrastructure/Repositories/XApiStatementRepository.cs`

**Step 1: Create LearningContentRepository**

```csharp
// src/backend/JLT.Infrastructure/Repositories/LearningContentRepository.cs
using JLT.Domain.Entities;
using JLT.Domain.Enums;
using JLT.Domain.Interfaces;
using JLT.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JLT.Infrastructure.Repositories;

public class LearningContentRepository : GenericRepository<LearningContent>, ILearningContentRepository
{
    public LearningContentRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<LearningContent>> GetByContentTypeAsync(ContentType type, CancellationToken ct = default)
    {
        return await _dbSet.Where(c => c.ContentType == type).AsNoTracking().ToListAsync(ct);
    }

    public async Task<IReadOnlyList<LearningContent>> GetByStatusAsync(ContentStatus status, CancellationToken ct = default)
    {
        return await _dbSet.Where(c => c.Status == status).AsNoTracking().ToListAsync(ct);
    }

    public async Task<IReadOnlyList<LearningContent>> GetExpiredContentAsync(DateTime now, CancellationToken ct = default)
    {
        return await _dbSet
            .Where(c => c.ValidTill.HasValue && c.ValidTill.Value <= now && c.Status == ContentStatus.Published)
            .ToListAsync(ct);
    }
}
```

**Step 2: Create ContentProgressRepository**

```csharp
// src/backend/JLT.Infrastructure/Repositories/ContentProgressRepository.cs
using JLT.Domain.Entities;
using JLT.Domain.Interfaces;
using JLT.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JLT.Infrastructure.Repositories;

public class ContentProgressRepository : GenericRepository<ContentProgress>, IContentProgressRepository
{
    public ContentProgressRepository(AppDbContext context) : base(context) { }

    public async Task<ContentProgress?> GetByUserAndContentAsync(Guid userId, Guid contentId, CancellationToken ct = default)
    {
        return await _dbSet.FirstOrDefaultAsync(p => p.UserId == userId && p.LearningContentId == contentId, ct);
    }

    public async Task<IReadOnlyList<ContentProgress>> GetByUserAsync(Guid userId, CancellationToken ct = default)
    {
        return await _dbSet.Where(p => p.UserId == userId).Include(p => p.LearningContent).AsNoTracking().ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ContentProgress>> GetByContentAsync(Guid contentId, CancellationToken ct = default)
    {
        return await _dbSet.Where(p => p.LearningContentId == contentId).AsNoTracking().ToListAsync(ct);
    }
}
```

**Step 3: Create ScormRepository**

```csharp
// src/backend/JLT.Infrastructure/Repositories/ScormRepository.cs
using JLT.Domain.Entities;
using JLT.Domain.Interfaces;
using JLT.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JLT.Infrastructure.Repositories;

public class ScormRepository : IScormRepository
{
    private readonly AppDbContext _context;

    public ScormRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ScormPackage?> GetPackageByContentIdAsync(Guid learningContentId, CancellationToken ct = default)
    {
        return await _context.ScormPackages
            .Include(s => s.RuntimeStates)
            .FirstOrDefaultAsync(s => s.LearningContentId == learningContentId, ct);
    }

    public async Task<ScormPackage> AddPackageAsync(ScormPackage package, CancellationToken ct = default)
    {
        await _context.ScormPackages.AddAsync(package, ct);
        await _context.SaveChangesAsync(ct);
        return package;
    }

    public async Task<ScormRuntimeState?> GetRuntimeStateAsync(Guid userId, Guid scormPackageId, CancellationToken ct = default)
    {
        return await _context.ScormRuntimeStates
            .FirstOrDefaultAsync(r => r.UserId == userId && r.ScormPackageId == scormPackageId, ct);
    }

    public async Task<ScormRuntimeState> UpsertRuntimeStateAsync(ScormRuntimeState state, CancellationToken ct = default)
    {
        var existing = await GetRuntimeStateAsync(state.UserId, state.ScormPackageId, ct);
        if (existing == null)
        {
            await _context.ScormRuntimeStates.AddAsync(state, ct);
        }
        else
        {
            existing.LessonStatus = state.LessonStatus;
            existing.LessonLocation = state.LessonLocation;
            existing.SuspendData = state.SuspendData;
            existing.RawScore = state.RawScore;
            existing.MinScore = state.MinScore;
            existing.MaxScore = state.MaxScore;
            existing.SessionTime = state.SessionTime;
            existing.TotalTime = state.TotalTime;
            existing.Entry = state.Entry;
        }
        await _context.SaveChangesAsync(ct);
        return existing ?? state;
    }
}
```

**Step 4: Create XApiStatementRepository**

```csharp
// src/backend/JLT.Infrastructure/Repositories/XApiStatementRepository.cs
using JLT.Domain.Entities;
using JLT.Domain.Interfaces;
using JLT.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JLT.Infrastructure.Repositories;

public class XApiStatementRepository : GenericRepository<XApiStatement>, IXApiStatementRepository
{
    public XApiStatementRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<XApiStatement>> GetByVerbAsync(string verbId, CancellationToken ct = default)
    {
        return await _dbSet.Where(x => x.VerbId == verbId).AsNoTracking().OrderByDescending(x => x.Timestamp).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<XApiStatement>> GetByActorAsync(string actorJson, CancellationToken ct = default)
    {
        // Uses JSONB containment: WHERE actor_json @> @actorJson
        return await _dbSet.Where(x => EF.Functions.JsonContains(x.ActorJson, actorJson)).AsNoTracking().OrderByDescending(x => x.Timestamp).ToListAsync(ct);
    }
}
```

**Step 5: Verify build**

Run: `dotnet build src/backend/JLT.Infrastructure/JLT.Infrastructure.csproj`
Expected: Build succeeded.

**Step 6: Commit**

```bash
git add src/backend/JLT.Infrastructure/Repositories/LearningContentRepository.cs src/backend/JLT.Infrastructure/Repositories/ContentProgressRepository.cs src/backend/JLT.Infrastructure/Repositories/ScormRepository.cs src/backend/JLT.Infrastructure/Repositories/XApiStatementRepository.cs
git commit -m "feat(lcm): add repository implementations for content, progress, SCORM, and xAPI"
```

---

## Task 8: Register LCM Repositories in DI

**Files:**
- Modify: `src/backend/JLT.Infrastructure/DependencyInjection.cs`

**Step 1: Add repository registrations**

After the existing `AddScoped<IUserGroupRepository, ...>()` line (line 27), add:

```csharp
        // LCM Repositories
        services.AddScoped<ILearningContentRepository, LearningContentRepository>();
        services.AddScoped<IContentProgressRepository, ContentProgressRepository>();
        services.AddScoped<IScormRepository, ScormRepository>();
        services.AddScoped<IXApiStatementRepository, XApiStatementRepository>();
```

Add the required `using` for `LearningContentRepository`, `ContentProgressRepository`, `ScormRepository`, `XApiStatementRepository` — they should resolve from the existing `using JLT.Infrastructure.Repositories;`.

**Step 2: Verify build**

Run: `dotnet build src/backend/JLT.Infrastructure/JLT.Infrastructure.csproj`
Expected: Build succeeded.

**Step 3: Commit**

```bash
git add src/backend/JLT.Infrastructure/DependencyInjection.cs
git commit -m "feat(lcm): register LCM repositories in dependency injection"
```

---

## Task 9: EF Core Migration

**Files:**
- Creates migration files in: `src/backend/JLT.Infrastructure/Migrations/`

**Step 1: Generate migration**

Run from repo root:

```bash
dotnet ef migrations add AddLearningContentManagement --project src/backend/JLT.Infrastructure --startup-project src/backend/JLT.API
```

Expected: Migration file created in `Migrations/` directory.

**Step 2: Review migration**

Open the generated migration file and verify:
- Tables created: `learning_content`, `content_tags`, `content_progress`, `scorm_packages`, `scorm_runtime_states`, `xapi_statements`
- All JSONB columns use correct type
- All indexes present
- Foreign keys correct

**Step 3: Apply migration**

```bash
dotnet ef database update --project src/backend/JLT.Infrastructure --startup-project src/backend/JLT.API
```

Expected: Database updated successfully.

**Step 4: Commit**

```bash
git add src/backend/JLT.Infrastructure/Migrations/
git commit -m "feat(lcm): add EF Core migration for LCM tables"
```

---

## Task 10: Application DTOs for LCM

**Files:**
- Create: `src/backend/JLT.Application/DTOs/LearningContentDtos.cs`

**Step 1: Create all LCM DTOs**

```csharp
// src/backend/JLT.Application/DTOs/LearningContentDtos.cs
namespace JLT.Application.DTOs;

public record LearningContentDto(
    Guid Id,
    Guid TenantId,
    string Title,
    string? Description,
    string ContentType,
    string Status,
    string Version,
    string? MimeType,
    string? StorageUrl,
    long? FileSize,
    int? DurationSeconds,
    string? ExternalUrl,
    string? Config,
    string? ThumbnailUrl,
    Guid CreatedBy,
    Guid UpdatedBy,
    DateTime? PublishedAt,
    DateTime? ValidFrom,
    DateTime? ValidTill,
    DateTime? RetiredAt,
    DateTime? NextReviewDate,
    Guid? ReviewedBy,
    DateTime? ReviewedAt,
    string ContentSource,
    string? SourceUrl,
    string? Author,
    string? Publisher,
    string? Copyright,
    string? LicenseType,
    string Language,
    string? Locale,
    int? EstimatedDurationMinutes,
    string? Category,
    string? Tags,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record LearningContentSummaryDto(
    Guid Id,
    string Title,
    string ContentType,
    string Status,
    string? ThumbnailUrl,
    string? Category,
    string Language,
    int? EstimatedDurationMinutes,
    string ContentSource,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record ContentProgressDto(
    Guid Id,
    Guid UserId,
    Guid LearningContentId,
    string? ContentTitle,
    string Status,
    decimal ProgressPercent,
    string? BookmarkData,
    int TimeSpentSeconds,
    DateTime? CompletedAt,
    DateTime LastAccessedAt
);

public record ScormRuntimeStateDto(
    Guid Id,
    Guid UserId,
    Guid ScormPackageId,
    string LessonStatus,
    string? LessonLocation,
    string? SuspendData,
    decimal? RawScore,
    decimal? MinScore,
    decimal? MaxScore,
    string? SessionTime,
    string? TotalTime,
    string? Entry
);

public record XApiStatementDto(
    Guid Id,
    string ActorJson,
    string VerbId,
    string ObjectJson,
    string? ResultJson,
    string? ContextJson,
    DateTime Timestamp,
    DateTime StoredAt
);
```

**Step 2: Verify build**

Run: `dotnet build src/backend/JLT.Application/JLT.Application.csproj`
Expected: Build succeeded.

**Step 3: Commit**

```bash
git add src/backend/JLT.Application/DTOs/LearningContentDtos.cs
git commit -m "feat(lcm): add DTOs for learning content, progress, SCORM, and xAPI"
```

---

## Task 11: Application Feature — LearningContent CRUD Commands

**Files:**
- Create: `src/backend/JLT.Application/Features/LearningContent/LearningContentCommands.cs`

**Step 1: Create commands file with Create, Update, Delete, and Status transition handlers**

This file follows the single-file pattern used by `UserCommands.cs`. Include:

1. **CreateLearningContentCommand** + Validator + Handler
   - Required: Title, ContentType, CreatedBy
   - Defaults: Status=Draft, Version="1.0", Language="en", ContentSource=Internal
   - Returns: `LearningContentDto`

2. **UpdateLearningContentCommand** + Validator + Handler
   - All fields optional except Id
   - Returns: `LearningContentDto`

3. **UpdateContentStatusCommand** + Handler
   - Input: Id, NewStatus
   - Logic: Validate state transition (see state machine in design doc). Set PublishedAt when transitioning to Published. Set RetiredAt when transitioning to Archived.
   - Returns: `LearningContentDto`

4. **DeleteLearningContentCommand** + Handler
   - Only allowed when Status is Draft
   - Returns: bool

5. **GetLearningContentByIdQuery** + Handler → `LearningContentDto?`

6. **GetLearningContentListQuery** + Handler → `PaginatedList<LearningContentSummaryDto>`
   - Filters: ContentType?, Status?, Category?, Language?, SearchTerm?, SortColumn?, SortOrder?

The full code is approximately 350-400 lines. Key implementation notes:

- Map entity to DTO using manual mapping (same pattern as `UserCommands.cs`)
- Use `ILearningContentRepository` for data access
- `UpdateContentStatusCommand` must enforce valid transitions:
  - `Draft` → `InReview`
  - `InReview` → `Published` | `Draft` (reject back)
  - `Published` → `Archived`
  - No manual transition to `Expired` (system-only)

**Step 2: Verify build**

Run: `dotnet build src/backend/JLT.Application/JLT.Application.csproj`
Expected: Build succeeded.

**Step 3: Commit**

```bash
git add src/backend/JLT.Application/Features/LearningContent/LearningContentCommands.cs
git commit -m "feat(lcm): add CRUD and status transition command/query handlers for LearningContent"
```

---

## Task 12: Application Feature — Content Progress Commands

**Files:**
- Create: `src/backend/JLT.Application/Features/LearningContent/ContentProgressCommands.cs`

**Step 1: Create progress tracking handlers**

Include:

1. **UpsertContentProgressCommand** + Handler
   - Input: UserId, LearningContentId, ProgressPercent, BookmarkData?, TimeSpentSeconds
   - Logic: Find existing progress record by (UserId, ContentId). If none, create new. Update ProgressPercent, BookmarkData, TimeSpentSeconds, LastAccessedAt. If ProgressPercent >= 100, set Status=Completed and CompletedAt. Else if > 0, set Status=InProgress.
   - Returns: `ContentProgressDto`

2. **GetContentProgressQuery** + Handler
   - Input: UserId, LearningContentId
   - Returns: `ContentProgressDto?`

3. **GetUserProgressListQuery** + Handler
   - Input: UserId
   - Returns: `List<ContentProgressDto>`

**Step 2: Verify build**

Run: `dotnet build src/backend/JLT.Application/JLT.Application.csproj`
Expected: Build succeeded.

**Step 3: Commit**

```bash
git add src/backend/JLT.Application/Features/LearningContent/ContentProgressCommands.cs
git commit -m "feat(lcm): add content progress upsert and query handlers"
```

---

## Task 13: Application Feature — SCORM Runtime Commands

**Files:**
- Create: `src/backend/JLT.Application/Features/LearningContent/ScormCommands.cs`

**Step 1: Create SCORM handlers**

Include:

1. **GetScormRuntimeStateQuery** + Handler
   - Input: UserId, LearningContentId
   - Logic: Find ScormPackage by ContentId, then find RuntimeState by (UserId, PackageId)
   - Returns: `ScormRuntimeStateDto?`

2. **UpsertScormRuntimeStateCommand** + Handler
   - Input: UserId, LearningContentId, LessonStatus, LessonLocation?, SuspendData?, scores, times, Entry?
   - Logic: Find package. Upsert runtime state.
   - Returns: `ScormRuntimeStateDto`

**Step 2: Verify build**

Run: `dotnet build src/backend/JLT.Application/JLT.Application.csproj`
Expected: Build succeeded.

**Step 3: Commit**

```bash
git add src/backend/JLT.Application/Features/LearningContent/ScormCommands.cs
git commit -m "feat(lcm): add SCORM runtime state query and upsert handlers"
```

---

## Task 14: Application Feature — xAPI Statement Commands

**Files:**
- Create: `src/backend/JLT.Application/Features/LearningContent/XApiCommands.cs`

**Step 1: Create xAPI handlers**

Include:

1. **StoreXApiStatementCommand** + Handler
   - Input: ActorJson, VerbId, ObjectJson, ResultJson?, ContextJson?, Timestamp?
   - Logic: Create immutable statement record. Set StoredAt = now.
   - Returns: `XApiStatementDto`

2. **GetXApiStatementsQuery** + Handler
   - Input: VerbId?, ActorJson?, page/pageSize
   - Returns: `PaginatedList<XApiStatementDto>`

**Step 2: Verify build**

Run: `dotnet build src/backend/JLT.Application/JLT.Application.csproj`
Expected: Build succeeded.

**Step 3: Commit**

```bash
git add src/backend/JLT.Application/Features/LearningContent/XApiCommands.cs
git commit -m "feat(lcm): add xAPI statement store and query handlers"
```

---

## Task 15: API Controller — LearningContentController

**Files:**
- Create: `src/backend/JLT.API/Controllers/LearningContentController.cs`

**Step 1: Create controller**

```csharp
// src/backend/JLT.API/Controllers/LearningContentController.cs
using JLT.Application.Common;
using JLT.Application.Common.Models;
using JLT.Application.DTOs;
using JLT.Application.Features.LearningContent;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JLT.API.Controllers;

[ApiController]
[Route("api/learning-content")]
[Authorize]
public class LearningContentController : ControllerBase
{
    private readonly IMediator _mediator;

    public LearningContentController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET /api/learning-content
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? contentType = null,
        [FromQuery] string? status = null,
        [FromQuery] string? category = null,
        [FromQuery] string? language = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? sortColumn = null,
        [FromQuery] string? sortOrder = null)
    {
        var query = new GetLearningContentListQuery(
            pageNumber, pageSize, contentType, status, category, language, searchTerm, sortColumn, sortOrder);
        var result = await _mediator.Send(query);
        return Ok(ApiResponse<PaginatedList<LearningContentSummaryDto>>.Ok(result));
    }

    // GET /api/learning-content/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetLearningContentByIdQuery(id));
        if (result == null) return NotFound(ApiResponse.Fail("Content not found."));
        return Ok(ApiResponse<LearningContentDto>.Ok(result));
    }

    // POST /api/learning-content
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLearningContentCommand command)
    {
        var result = await _mediator.Send(command);
        return Created($"/api/learning-content/{result.Id}", ApiResponse<LearningContentDto>.Ok(result, "Content created."));
    }

    // PUT /api/learning-content/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLearningContentCommand command)
    {
        if (id != command.Id) return BadRequest(ApiResponse.Fail("ID mismatch."));
        var result = await _mediator.Send(command);
        return Ok(ApiResponse<LearningContentDto>.Ok(result, "Content updated."));
    }

    // PATCH /api/learning-content/{id}/status
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateContentStatusCommand command)
    {
        if (id != command.Id) return BadRequest(ApiResponse.Fail("ID mismatch."));
        var result = await _mediator.Send(command);
        return Ok(ApiResponse<LearningContentDto>.Ok(result, $"Status changed to {result.Status}."));
    }

    // DELETE /api/learning-content/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteLearningContentCommand(id));
        if (!result) return BadRequest(ApiResponse.Fail("Only draft content can be deleted."));
        return Ok(ApiResponse.Ok("Content deleted."));
    }
}
```

**Step 2: Verify build**

Run: `dotnet build src/backend/JLT.API/JLT.API.csproj`
Expected: Build succeeded.

**Step 3: Commit**

```bash
git add src/backend/JLT.API/Controllers/LearningContentController.cs
git commit -m "feat(lcm): add LearningContentController with CRUD and status endpoints"
```

---

## Task 16: API Controller — ContentProgressController

**Files:**
- Create: `src/backend/JLT.API/Controllers/ContentProgressController.cs`

**Step 1: Create controller**

```csharp
// src/backend/JLT.API/Controllers/ContentProgressController.cs
using System.Security.Claims;
using JLT.Application.Common;
using JLT.Application.DTOs;
using JLT.Application.Features.LearningContent;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JLT.API.Controllers;

[ApiController]
[Route("api/content-progress")]
[Authorize]
public class ContentProgressController : ControllerBase
{
    private readonly IMediator _mediator;

    public ContentProgressController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET /api/content-progress/me
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProgress()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized(ApiResponse.Fail("Invalid token."));
        var result = await _mediator.Send(new GetUserProgressListQuery(userId.Value));
        return Ok(ApiResponse<List<ContentProgressDto>>.Ok(result));
    }

    // GET /api/content-progress/{contentId}
    [HttpGet("{contentId:guid}")]
    public async Task<IActionResult> GetProgress(Guid contentId)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized(ApiResponse.Fail("Invalid token."));
        var result = await _mediator.Send(new GetContentProgressQuery(userId.Value, contentId));
        if (result == null) return NotFound(ApiResponse.Fail("No progress found."));
        return Ok(ApiResponse<ContentProgressDto>.Ok(result));
    }

    // PUT /api/content-progress/{contentId}
    [HttpPut("{contentId:guid}")]
    public async Task<IActionResult> UpsertProgress(Guid contentId, [FromBody] UpsertContentProgressRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized(ApiResponse.Fail("Invalid token."));
        var command = new UpsertContentProgressCommand(
            userId.Value, contentId, request.ProgressPercent, request.BookmarkData, request.TimeSpentSeconds);
        var result = await _mediator.Send(command);
        return Ok(ApiResponse<ContentProgressDto>.Ok(result, "Progress updated."));
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}

public record UpsertContentProgressRequest(decimal ProgressPercent, string? BookmarkData, int TimeSpentSeconds);
```

**Step 2: Verify build**

Run: `dotnet build src/backend/JLT.API/JLT.API.csproj`
Expected: Build succeeded.

**Step 3: Commit**

```bash
git add src/backend/JLT.API/Controllers/ContentProgressController.cs
git commit -m "feat(lcm): add ContentProgressController with get/upsert endpoints"
```

---

## Task 17: API Controller — ScormController

**Files:**
- Create: `src/backend/JLT.API/Controllers/ScormController.cs`

**Step 1: Create controller**

```csharp
// src/backend/JLT.API/Controllers/ScormController.cs
using System.Security.Claims;
using JLT.Application.Common;
using JLT.Application.DTOs;
using JLT.Application.Features.LearningContent;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JLT.API.Controllers;

[ApiController]
[Route("api/scorm")]
[Authorize]
public class ScormController : ControllerBase
{
    private readonly IMediator _mediator;

    public ScormController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET /api/scorm/{contentId}/runtime
    [HttpGet("{contentId:guid}/runtime")]
    public async Task<IActionResult> GetRuntimeState(Guid contentId)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized(ApiResponse.Fail("Invalid token."));
        var result = await _mediator.Send(new GetScormRuntimeStateQuery(userId.Value, contentId));
        if (result == null) return NotFound(ApiResponse.Fail("No SCORM runtime state found."));
        return Ok(ApiResponse<ScormRuntimeStateDto>.Ok(result));
    }

    // PUT /api/scorm/{contentId}/runtime
    [HttpPut("{contentId:guid}/runtime")]
    public async Task<IActionResult> UpsertRuntimeState(Guid contentId, [FromBody] UpsertScormRuntimeStateCommand command)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized(ApiResponse.Fail("Invalid token."));
        // Override userId and contentId from route/token
        var cmd = command with { UserId = userId.Value, LearningContentId = contentId };
        var result = await _mediator.Send(cmd);
        return Ok(ApiResponse<ScormRuntimeStateDto>.Ok(result, "SCORM runtime state updated."));
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
```

**Step 2: Verify build**

Run: `dotnet build src/backend/JLT.API/JLT.API.csproj`
Expected: Build succeeded.

**Step 3: Commit**

```bash
git add src/backend/JLT.API/Controllers/ScormController.cs
git commit -m "feat(lcm): add ScormController with runtime state get/upsert endpoints"
```

---

## Task 18: API Controller — XApiController

**Files:**
- Create: `src/backend/JLT.API/Controllers/XApiController.cs`

**Step 1: Create controller**

```csharp
// src/backend/JLT.API/Controllers/XApiController.cs
using JLT.Application.Common;
using JLT.Application.Common.Models;
using JLT.Application.DTOs;
using JLT.Application.Features.LearningContent;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JLT.API.Controllers;

[ApiController]
[Route("api/xapi/statements")]
[Authorize]
public class XApiController : ControllerBase
{
    private readonly IMediator _mediator;

    public XApiController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // POST /api/xapi/statements
    [HttpPost]
    public async Task<IActionResult> StoreStatement([FromBody] StoreXApiStatementCommand command)
    {
        var result = await _mediator.Send(command);
        return Created($"/api/xapi/statements/{result.Id}", ApiResponse<XApiStatementDto>.Ok(result, "Statement stored."));
    }

    // GET /api/xapi/statements
    [HttpGet]
    public async Task<IActionResult> GetStatements(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? verbId = null,
        [FromQuery] string? actorJson = null)
    {
        var query = new GetXApiStatementsQuery(pageNumber, pageSize, verbId, actorJson);
        var result = await _mediator.Send(query);
        return Ok(ApiResponse<PaginatedList<XApiStatementDto>>.Ok(result));
    }
}
```

**Step 2: Verify build**

Run: `dotnet build src/backend/JLT.API/JLT.API.csproj`
Expected: Build succeeded.

**Step 3: Commit**

```bash
git add src/backend/JLT.API/Controllers/XApiController.cs
git commit -m "feat(lcm): add XApiController with statement store and list endpoints"
```

---

## Task 19: Unit Tests — Domain & Handler Tests

**Files:**
- Create: `src/backend/JLT.Tests/LearningContentTests.cs`

**Step 1: Write tests**

Tests to cover:

1. **Entity defaults**: LearningContent defaults to Draft status, "1.0" version, "en" language, Internal source
2. **ContentStatus transitions**: Valid transitions succeed, invalid ones throw
3. **CreateLearningContentCommand handler**: Creates with correct defaults, validates Title required
4. **UpdateContentStatusCommand handler**: Tests Draft→InReview, InReview→Published (sets PublishedAt), Published→Archived (sets RetiredAt), rejects Published→Draft
5. **UpsertContentProgressCommand handler**: Creates new record, updates existing, auto-completes at 100%
6. **DeleteLearningContentCommand handler**: Succeeds for Draft, fails for Published

Use the same test patterns as the existing test files (xUnit + Moq). Mock `ILearningContentRepository`, `IContentProgressRepository`.

**Step 2: Run tests**

Run: `dotnet test src/backend/JLT.Tests/ --filter "FullyQualifiedName~LearningContentTests" -v normal`
Expected: All tests pass.

**Step 3: Commit**

```bash
git add src/backend/JLT.Tests/LearningContentTests.cs
git commit -m "test(lcm): add unit tests for LearningContent handlers and status transitions"
```

---

## Task 20: Integration Tests — API Endpoints

**Files:**
- Create: `src/backend/JLT.Tests/LearningContentIntegrationTests.cs`

**Step 1: Write integration tests**

Follow the same pattern as `IntegrationTests.cs`. Start the API locally, use `HttpClient`:

1. **POST /api/learning-content** — Create document content, verify 201
2. **GET /api/learning-content/{id}** — Retrieve created content, verify all fields
3. **GET /api/learning-content** — List with filters (contentType=Document), verify pagination
4. **PUT /api/learning-content/{id}** — Update title and category, verify changes
5. **PATCH /api/learning-content/{id}/status** — Transition Draft→InReview→Published, verify PublishedAt set
6. **PUT /api/content-progress/{contentId}** — Upsert progress, verify bookmark stored
7. **GET /api/content-progress/{contentId}** — Verify progress retrieval
8. **DELETE /api/learning-content/{id}** — Create draft, delete, verify 200. Try delete published, verify 400.

**Step 2: Start API**

Run: `dotnet run --project src/backend/JLT.API` (background)

**Step 3: Run integration tests**

Run: `dotnet test src/backend/JLT.Tests/ --filter "FullyQualifiedName~LearningContentIntegrationTests" -v normal`
Expected: All tests pass.

**Step 4: Commit**

```bash
git add src/backend/JLT.Tests/LearningContentIntegrationTests.cs
git commit -m "test(lcm): add integration tests for LCM API endpoints"
```

---

## API Endpoint Summary

| Method | Route | Purpose |
|--------|-------|---------|
| `GET` | `/api/learning-content` | List content (paginated, filterable) |
| `GET` | `/api/learning-content/{id}` | Get content by ID |
| `POST` | `/api/learning-content` | Create content |
| `PUT` | `/api/learning-content/{id}` | Update content |
| `PATCH` | `/api/learning-content/{id}/status` | Change status (lifecycle transition) |
| `DELETE` | `/api/learning-content/{id}` | Delete draft content |
| `GET` | `/api/content-progress/me` | Get all progress for current user |
| `GET` | `/api/content-progress/{contentId}` | Get progress for specific content |
| `PUT` | `/api/content-progress/{contentId}` | Upsert progress + bookmark |
| `GET` | `/api/scorm/{contentId}/runtime` | Get SCORM runtime state |
| `PUT` | `/api/scorm/{contentId}/runtime` | Upsert SCORM runtime state |
| `POST` | `/api/xapi/statements` | Store xAPI statement |
| `GET` | `/api/xapi/statements` | List xAPI statements |
