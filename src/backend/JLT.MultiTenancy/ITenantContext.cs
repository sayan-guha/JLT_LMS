namespace JLT.MultiTenancy;

public interface ITenantContext
{
    Guid? TenantId { get; }
    string? TenantSlug { get; }
    bool IsResolved { get; }
    void SetTenant(Guid tenantId, string slug);
    void Clear();
}
