using JLT.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JLT.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(300);
        builder.Property(t => t.Slug).IsRequired().HasMaxLength(100);
        builder.HasIndex(t => t.Slug).IsUnique();
        builder.Property(t => t.Domain).HasMaxLength(255);
        builder.Property(t => t.PlanType).HasMaxLength(50).HasDefaultValue("standard");
        builder.Property(t => t.PrimaryColor).HasMaxLength(7);
        builder.Property(t => t.SecondaryColor).HasMaxLength(7);
        builder.Property(t => t.Settings).HasColumnType("jsonb");
        builder.Property(t => t.IsActive).HasDefaultValue(true);

        builder.HasMany(t => t.Features)
            .WithOne(f => f.Tenant)
            .HasForeignKey(f => f.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class TenantFeatureConfiguration : IEntityTypeConfiguration<TenantFeature>
{
    public void Configure(EntityTypeBuilder<TenantFeature> builder)
    {
        builder.ToTable("tenant_features");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.FeatureKey).IsRequired().HasMaxLength(200);
        builder.Property(f => f.Config).HasColumnType("jsonb");
        builder.HasIndex(f => new { f.TenantId, f.FeatureKey }).IsUnique();
    }
}

public class SuperAdminConfiguration : IEntityTypeConfiguration<SuperAdmin>
{
    public void Configure(EntityTypeBuilder<SuperAdmin> builder)
    {
        builder.ToTable("super_admins");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Email).IsRequired().HasMaxLength(255);
        builder.HasIndex(s => s.Email).IsUnique();
        builder.Property(s => s.PasswordHash).IsRequired();
        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
        builder.Property(s => s.IsActive).HasDefaultValue(true);
    }
}
