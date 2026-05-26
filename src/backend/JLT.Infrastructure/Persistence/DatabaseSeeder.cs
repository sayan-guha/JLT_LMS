using JLT.Domain.Entities;
using JLT.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JLT.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        try
        {
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migration completed.");

            await SeedPermissionsAsync(context, logger);
            await SeedSuperAdminAsync(context, passwordHasher, logger);
            await SeedDemoTenantAsync(context, passwordHasher, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    private static async Task SeedPermissionsAsync(AppDbContext context, ILogger logger)
    {
        if (await context.Permissions.AnyAsync())
            return;

        var permissions = new List<Permission>
        {
            // Users
            new() { Key = "users.view", Name = "View Users", Category = "Users" },
            new() { Key = "users.create", Name = "Create Users", Category = "Users" },
            new() { Key = "users.update", Name = "Update Users", Category = "Users" },
            new() { Key = "users.delete", Name = "Delete Users", Category = "Users" },
            new() { Key = "users.bulk_update", Name = "Bulk Update Users", Category = "Users" },
            new() { Key = "users.activate", Name = "Activate/Deactivate Users", Category = "Users" },
            new() { Key = "users.assign_roles", Name = "Assign Roles to Users", Category = "Users" },

            // Roles
            new() { Key = "roles.view", Name = "View Roles", Category = "Roles" },
            new() { Key = "roles.create", Name = "Create Roles", Category = "Roles" },
            new() { Key = "roles.update", Name = "Update Roles", Category = "Roles" },
            new() { Key = "roles.delete", Name = "Delete Roles", Category = "Roles" },
            new() { Key = "roles.assign_permissions", Name = "Assign Permissions", Category = "Roles" },

            // User Groups
            new() { Key = "groups.view", Name = "View Groups", Category = "Groups" },
            new() { Key = "groups.create", Name = "Create Groups", Category = "Groups" },
            new() { Key = "groups.update", Name = "Update Groups", Category = "Groups" },
            new() { Key = "groups.delete", Name = "Delete Groups", Category = "Groups" },
            new() { Key = "groups.manage_members", Name = "Manage Group Members", Category = "Groups" },

            // Dynamic Fields
            new() { Key = "fields.view", Name = "View Dynamic Fields", Category = "Dynamic Fields" },
            new() { Key = "fields.create", Name = "Create Dynamic Fields", Category = "Dynamic Fields" },
            new() { Key = "fields.update", Name = "Update Dynamic Fields", Category = "Dynamic Fields" },
            new() { Key = "fields.delete", Name = "Delete Dynamic Fields", Category = "Dynamic Fields" },

            // Tenant Admin
            new() { Key = "tenant.settings", Name = "Manage Tenant Settings", Category = "Tenant" },
            new() { Key = "tenant.branding", Name = "Manage Tenant Branding", Category = "Tenant" },
            new() { Key = "tenant.features", Name = "Manage Tenant Features", Category = "Tenant" },

            // Audit
            new() { Key = "audit.view", Name = "View Audit Logs", Category = "Audit" },

            // Future: Courses, Content, Assessments
            new() { Key = "courses.view", Name = "View Courses", Category = "Courses" },
            new() { Key = "courses.create", Name = "Create Courses", Category = "Courses" },
            new() { Key = "courses.publish", Name = "Publish Courses", Category = "Courses" },
            new() { Key = "courses.assign", Name = "Assign Courses", Category = "Courses" },
            new() { Key = "content.upload", Name = "Upload Content", Category = "Content" },
            new() { Key = "content.manage", Name = "Manage Content", Category = "Content" },
            new() { Key = "assessments.create", Name = "Create Assessments", Category = "Assessments" },
            new() { Key = "assessments.view", Name = "View Assessments", Category = "Assessments" },
            new() { Key = "reports.view", Name = "View Reports", Category = "Reports" },
            new() { Key = "reports.export", Name = "Export Reports", Category = "Reports" },
        };

        context.Permissions.AddRange(permissions);
        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} permissions.", permissions.Count);
    }

    private static async Task SeedSuperAdminAsync(AppDbContext context, IPasswordHasher passwordHasher, ILogger logger)
    {
        if (await context.SuperAdmins.AnyAsync())
            return;

        var superAdmin = new SuperAdmin
        {
            Email = "admin@jlt.platform",
            PasswordHash = passwordHasher.Hash("Admin@123!"),
            Name = "Platform Admin",
            IsActive = true
        };

        context.SuperAdmins.Add(superAdmin);
        await context.SaveChangesAsync();
        logger.LogInformation("Seeded super admin: {Email}", superAdmin.Email);
    }

    private static async Task SeedDemoTenantAsync(AppDbContext context, IPasswordHasher passwordHasher, ILogger logger)
    {
        var demoTenant = await context.Tenants.FirstOrDefaultAsync(t => t.Slug == "demo");
        if (demoTenant == null)
        {
            demoTenant = new Tenant
            {
                Id = new Guid("d3b07384-d113-49be-a5d6-d069e224e756"),
                Name = "Demo Organization",
                Slug = "demo",
                Domain = "demo.jlt.platform",
                PlanType = "standard",
                IsActive = true
            };
            context.Tenants.Add(demoTenant);
            await context.SaveChangesAsync();


            context.TenantFeatures.AddRange(
                new TenantFeature { TenantId = demoTenant.Id, FeatureKey = "scorm_enabled", IsEnabled = true, Config = "{}" },
                new TenantFeature { TenantId = demoTenant.Id, FeatureKey = "ai_chat_enabled", IsEnabled = true, Config = "{}" }
            );

            var adminRole = new Role { TenantId = demoTenant.Id, Name = "Admin", Description = "Tenant Administrator", IsSystemRole = true, IsActive = true };
            var managerRole = new Role { TenantId = demoTenant.Id, Name = "Manager", Description = "Tenant Manager", IsSystemRole = true, IsActive = true };
            var instructorRole = new Role { TenantId = demoTenant.Id, Name = "Instructor", Description = "Course Instructor", IsSystemRole = true, IsActive = true };
            var learnerRole = new Role { TenantId = demoTenant.Id, Name = "Learner", Description = "Course Learner", IsSystemRole = true, IsActive = true };

            context.Roles.AddRange(adminRole, managerRole, instructorRole, learnerRole);
            await context.SaveChangesAsync();

            var allPermissions = await context.Permissions.ToListAsync();
            foreach (var perm in allPermissions)
            {
                context.RolePermissions.Add(new RolePermission { RoleId = adminRole.Id, PermissionId = perm.Id });
            }
            await context.SaveChangesAsync();

            var demoAdmin = new User
            {
                TenantId = demoTenant.Id,
                Email = "admin@demo.com",
                PasswordHash = passwordHasher.Hash("Admin@123!"),
                FirstName = "Demo",
                LastName = "Admin",
                Department = "Operations",
                JobTitle = "System Administrator",
                Location = "Headquarters",
                IsActive = true
            };

            context.Users.Add(demoAdmin);
            await context.SaveChangesAsync();

            context.UserRoles.Add(new UserRole { UserId = demoAdmin.Id, RoleId = adminRole.Id });
            await context.SaveChangesAsync();

            logger.LogInformation("Seeded demo tenant: {Slug}", demoTenant.Slug);
        }
    }
}
