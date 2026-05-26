using JLT.Domain.Common;

namespace JLT.Domain.Entities;

public class TenantFeature : BaseEntity
{
    public Guid TenantId { get; set; }
    public string FeatureKey { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public string? Config { get; set; }

    // Navigation
    public virtual Tenant? Tenant { get; set; }
}
