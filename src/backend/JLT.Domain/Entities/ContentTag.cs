using JLT.Domain.Common;

namespace JLT.Domain.Entities;

public class ContentTag : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
}
