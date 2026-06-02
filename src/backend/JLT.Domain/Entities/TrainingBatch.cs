using System;
using System.Collections.Generic;
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
