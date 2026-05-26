using JLT.Domain.Common;
using JLT.Domain.Enums;

namespace JLT.Domain.Entities;

public class PhysicalResource : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ResourceType Type { get; set; }
    public string? Location { get; set; }
    public int? Capacity { get; set; }
    public bool IsActive { get; set; } = true;
}
