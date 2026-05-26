using System;
using JLT.Domain.Common;

namespace JLT.Domain.Entities;

public class InstructorProfile : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string? Bio { get; set; }
    public string? Specializations { get; set; } // jsonb
    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual User? User { get; set; }
}
