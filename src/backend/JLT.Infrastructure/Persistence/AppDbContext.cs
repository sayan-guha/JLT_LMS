using JLT.Domain.Common;
using JLT.Domain.Entities;
using JLT.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace JLT.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    // Global tables
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantFeature> TenantFeatures => Set<TenantFeature>();
    public DbSet<SuperAdmin> SuperAdmins => Set<SuperAdmin>();
    public DbSet<Permission> Permissions => Set<Permission>();

    // Tenant-scoped tables
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<DynamicFieldDefinition> DynamicFieldDefinitions => Set<DynamicFieldDefinition>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserGroup> UserGroups => Set<UserGroup>();
    public DbSet<UserGroupMember> UserGroupMembers => Set<UserGroupMember>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // Learning Content Management
    public DbSet<LearningContent> LearningContent => Set<LearningContent>();
    public DbSet<ContentTag> ContentTags => Set<ContentTag>();
    public DbSet<ContentProgress> ContentProgress => Set<ContentProgress>();
    public DbSet<ScormPackage> ScormPackages => Set<ScormPackage>();
    public DbSet<ScormRuntimeState> ScormRuntimeStates => Set<ScormRuntimeState>();
    public DbSet<XApiStatement> XApiStatements => Set<XApiStatement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Global query filters for tenant isolation
        modelBuilder.Entity<User>().HasQueryFilter(e => !_tenantContext.IsResolved || e.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<Role>().HasQueryFilter(e => !_tenantContext.IsResolved || e.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<DynamicFieldDefinition>().HasQueryFilter(e => !_tenantContext.IsResolved || e.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<UserGroup>().HasQueryFilter(e => !_tenantContext.IsResolved || e.TenantId == _tenantContext.TenantId);

        // LCM tenant isolation
        modelBuilder.Entity<LearningContent>().HasQueryFilter(e => !_tenantContext.IsResolved || e.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<ContentTag>().HasQueryFilter(e => !_tenantContext.IsResolved || e.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<XApiStatement>().HasQueryFilter(e => !_tenantContext.IsResolved || e.TenantId == _tenantContext.TenantId);
    }

    public override int SaveChanges()
    {
        ApplyAuditInfo();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInfo();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditInfo()
    {
        var entries = ChangeTracker.Entries<BaseEntity>();
        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    // Auto-set tenant_id for new tenant-scoped entities
                    if (entry.Entity is ITenantEntity tenantEntity && _tenantContext.IsResolved)
                    {
                        if (tenantEntity.TenantId == Guid.Empty)
                            tenantEntity.TenantId = _tenantContext.TenantId!.Value;
                    }
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }
    }
}
