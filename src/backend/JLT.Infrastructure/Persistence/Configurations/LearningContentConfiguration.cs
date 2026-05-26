using JLT.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JLT.Infrastructure.Persistence.Configurations;

public class LearningContentConfiguration : IEntityTypeConfiguration<LearningContent>
{
    public void Configure(EntityTypeBuilder<LearningContent> builder)
    {
        builder.ToTable("learning_content");
        builder.HasKey(c => c.Id);

        // Identity
        builder.Property(c => c.Title).IsRequired().HasMaxLength(500);
        builder.Property(c => c.Description).HasColumnType("text");
        builder.Property(c => c.ContentType).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.Version).IsRequired().HasMaxLength(20);

        // Storage
        builder.Property(c => c.MimeType).HasMaxLength(100);
        builder.Property(c => c.StorageUrl).HasMaxLength(2048);
        builder.Property(c => c.ExternalUrl).HasMaxLength(2048);
        builder.Property(c => c.Config).HasColumnType("jsonb");
        builder.Property(c => c.ThumbnailUrl).HasMaxLength(2048);

        // Lifecycle
        builder.Property(c => c.CreatedBy).IsRequired();
        builder.Property(c => c.UpdatedBy).IsRequired();

        // Provenance
        builder.Property(c => c.ContentSource).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.SourceUrl).HasMaxLength(2048);
        builder.Property(c => c.Author).HasMaxLength(200);
        builder.Property(c => c.Publisher).HasMaxLength(200);
        builder.Property(c => c.Copyright).HasMaxLength(500);
        builder.Property(c => c.LicenseType).HasMaxLength(100);

        // Discovery
        builder.Property(c => c.Language).IsRequired().HasMaxLength(10);
        builder.Property(c => c.Locale).HasMaxLength(10);
        builder.Property(c => c.Category).HasMaxLength(200);
        builder.Property(c => c.Tags).HasColumnType("jsonb");

        // Indexes
        builder.HasIndex(c => c.TenantId);
        builder.HasIndex(c => new { c.TenantId, c.Status });
        builder.HasIndex(c => new { c.TenantId, c.ContentType });
        builder.HasIndex(c => new { c.TenantId, c.Category });
        builder.HasIndex(c => c.Tags).HasMethod("gin").HasOperators("jsonb_path_ops");

        // Navigation: ScormPackage (1:1)
        builder.HasOne(c => c.ScormPackage)
            .WithOne(s => s.LearningContent)
            .HasForeignKey<ScormPackage>(s => s.LearningContentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Navigation: ContentProgress (1:many)
        builder.HasMany(c => c.ProgressRecords)
            .WithOne(p => p.LearningContent)
            .HasForeignKey(p => p.LearningContentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ContentTagConfiguration : IEntityTypeConfiguration<ContentTag>
{
    public void Configure(EntityTypeBuilder<ContentTag> builder)
    {
        builder.ToTable("content_tags");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(t => new { t.TenantId, t.Name }).IsUnique();
        builder.HasIndex(t => t.TenantId);
    }
}
