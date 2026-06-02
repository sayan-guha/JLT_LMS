using System;
using System.Collections.Generic;
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
