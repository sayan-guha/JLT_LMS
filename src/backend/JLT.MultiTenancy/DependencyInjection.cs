using Microsoft.Extensions.DependencyInjection;

namespace JLT.MultiTenancy;

public static class DependencyInjection
{
    public static IServiceCollection AddMultiTenancy(this IServiceCollection services)
    {
        services.AddScoped<ITenantContext, TenantContext>();
        return services;
    }
}
