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
