using JLT.Domain.Entities;
using JLT.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JLT.Infrastructure.Persistence.Configurations;

public class DynamicFieldDefinitionConfiguration : IEntityTypeConfiguration<DynamicFieldDefinition>
{
    public void Configure(EntityTypeBuilder<DynamicFieldDefinition> builder)
    {
        builder.ToTable("dynamic_field_definitions");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.FieldKey).IsRequired().HasMaxLength(200);
        builder.HasIndex(d => new { d.TenantId, d.FieldKey }).IsUnique();
        builder.Property(d => d.DisplayName).IsRequired().HasMaxLength(300);
        builder.Property(d => d.FieldType).HasConversion<string>().HasMaxLength(50);
        builder.Property(d => d.Options).HasColumnType("jsonb");
        builder.Property(d => d.DefaultValue).HasMaxLength(500);
        builder.Property(d => d.IsActive).HasDefaultValue(true);
    }
}

public class UserGroupConfiguration : IEntityTypeConfiguration<UserGroup>
{
    public void Configure(EntityTypeBuilder<UserGroup> builder)
    {
        builder.ToTable("user_groups");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Name).IsRequired().HasMaxLength(300);
        builder.HasIndex(g => g.TenantId);
        builder.Property(g => g.Type).HasConversion<string>().HasMaxLength(50);
        builder.Property(g => g.Rules).HasColumnType("jsonb");

        builder.HasOne(g => g.CreatedBy)
            .WithMany()
            .HasForeignKey(g => g.CreatedById)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(g => g.Members)
            .WithOne(m => m.Group)
            .HasForeignKey(m => m.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserGroupMemberConfiguration : IEntityTypeConfiguration<UserGroupMember>
{
    public void Configure(EntityTypeBuilder<UserGroupMember> builder)
    {
        builder.ToTable("user_group_members");
        builder.HasKey(m => m.Id);
        builder.HasIndex(m => new { m.GroupId, m.UserId }).IsUnique();
        builder.HasIndex(m => m.UserId);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).UseIdentityAlwaysColumn();
        builder.Property(a => a.Action).IsRequired().HasMaxLength(200);
        builder.Property(a => a.EntityType).HasMaxLength(100);
        builder.Property(a => a.OldValues).HasColumnType("jsonb");
        builder.Property(a => a.NewValues).HasColumnType("jsonb");
        builder.Property(a => a.IpAddress).HasMaxLength(45);
        builder.Property(a => a.Source).HasConversion<string>().HasMaxLength(50);
        builder.HasIndex(a => new { a.TenantId, a.CreatedAt });
        builder.HasIndex(a => new { a.EntityType, a.EntityId });
        builder.HasIndex(a => new { a.Action, a.CreatedAt });

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
