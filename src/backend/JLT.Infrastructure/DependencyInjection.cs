using JLT.Domain.Interfaces;
using JLT.Infrastructure.Persistence;
using JLT.Infrastructure.Repositories;
using JLT.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JLT.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
            .UseSnakeCaseNamingConvention());

        // Repositories
        services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IUserGroupRepository, UserGroupRepository>();

        // LCM Repositories
        services.AddScoped<ILearningContentRepository, LearningContentRepository>();
        services.AddScoped<IContentProgressRepository, ContentProgressRepository>();
        services.AddScoped<IScormRepository, ScormRepository>();
        services.AddScoped<IXApiStatementRepository, XApiStatementRepository>();

        // Services
        services.AddScoped<IPasswordHasher, PasswordHasherService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IDynamicFieldService, DynamicFieldService>();

        return services;
    }
}
