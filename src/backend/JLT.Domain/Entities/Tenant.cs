using JLT.Domain.Common;

namespace JLT.Domain.Entities;

public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Domain { get; set; }
    public string? LogoUrl { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string PlanType { get; set; } = "standard";
    public int? MaxUsers { get; set; }
    public int? MaxStorageGb { get; set; }
    public string? Settings { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual ICollection<TenantFeature> Features { get; set; } = new List<TenantFeature>();
}
