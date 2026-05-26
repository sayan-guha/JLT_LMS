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
