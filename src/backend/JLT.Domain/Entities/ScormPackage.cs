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
