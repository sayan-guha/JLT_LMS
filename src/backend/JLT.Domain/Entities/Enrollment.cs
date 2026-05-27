using System;
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
