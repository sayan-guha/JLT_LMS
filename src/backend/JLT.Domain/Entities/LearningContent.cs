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
