using JLT.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JLT.Infrastructure.Persistence.Configurations;

public class ContentProgressConfiguration : IEntityTypeConfiguration<ContentProgress>
{
    public void Configure(EntityTypeBuilder<ContentProgress> builder)
    {
        builder.ToTable("content_progress");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.ProgressPercent).HasPrecision(5, 2);
        builder.Property(p => p.BookmarkData).HasColumnType("jsonb");

        builder.HasIndex(p => new { p.UserId, p.LearningContentId }).IsUnique();
        builder.HasIndex(p => p.UserId);
        builder.HasIndex(p => p.LearningContentId);
    }
}

public class ScormPackageConfiguration : IEntityTypeConfiguration<ScormPackage>
{
    public void Configure(EntityTypeBuilder<ScormPackage> builder)
    {
        builder.ToTable("scorm_packages");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.EntryPoint).IsRequired().HasMaxLength(500);
        builder.Property(s => s.ScormVersion).IsRequired().HasMaxLength(20);
        builder.Property(s => s.ManifestData).IsRequired().HasColumnType("jsonb");

        builder.HasIndex(s => s.LearningContentId).IsUnique();

        builder.HasMany(s => s.RuntimeStates)
            .WithOne(r => r.ScormPackage)
            .HasForeignKey(r => r.ScormPackageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ScormRuntimeStateConfiguration : IEntityTypeConfiguration<ScormRuntimeState>
{
    public void Configure(EntityTypeBuilder<ScormRuntimeState> builder)
    {
        builder.ToTable("scorm_runtime_states");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.LessonStatus).IsRequired().HasMaxLength(50);
        builder.Property(r => r.LessonLocation).HasMaxLength(1000);
        builder.Property(r => r.SuspendData).HasColumnType("text");
        builder.Property(r => r.RawScore).HasPrecision(10, 2);
        builder.Property(r => r.MinScore).HasPrecision(10, 2);
        builder.Property(r => r.MaxScore).HasPrecision(10, 2);
        builder.Property(r => r.SessionTime).HasMaxLength(50);
        builder.Property(r => r.TotalTime).HasMaxLength(50);
        builder.Property(r => r.Entry).HasMaxLength(20);

        builder.HasIndex(r => new { r.UserId, r.ScormPackageId }).IsUnique();
        builder.HasIndex(r => r.UserId);
    }
}

public class XApiStatementConfiguration : IEntityTypeConfiguration<XApiStatement>
{
    public void Configure(EntityTypeBuilder<XApiStatement> builder)
    {
        builder.ToTable("xapi_statements");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ActorJson).IsRequired().HasColumnType("jsonb");
        builder.Property(x => x.VerbId).IsRequired().HasMaxLength(500);
        builder.Property(x => x.ObjectJson).IsRequired().HasColumnType("jsonb");
        builder.Property(x => x.ResultJson).HasColumnType("jsonb");
        builder.Property(x => x.ContextJson).HasColumnType("jsonb");

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.VerbId });
        builder.HasIndex(x => new { x.TenantId, x.Timestamp });
        builder.HasIndex(x => x.ActorJson).HasMethod("gin").HasOperators("jsonb_path_ops");
    }
}
