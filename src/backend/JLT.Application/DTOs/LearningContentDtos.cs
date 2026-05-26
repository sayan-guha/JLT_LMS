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
