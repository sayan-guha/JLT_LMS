using System;
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
