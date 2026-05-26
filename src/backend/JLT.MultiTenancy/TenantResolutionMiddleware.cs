using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using JLT.Domain.Interfaces;

namespace JLT.MultiTenancy;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly HashSet<string> TenantFreeEndpoints = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/super-admin",
        "/health",
        "/swagger",
        "/api/tenants"
    };

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";

        // Skip tenant resolution for tenant-free endpoints
        if (TenantFreeEndpoints.Any(e => path.StartsWith(e, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        var tenantContext = context.RequestServices.GetRequiredService<ITenantContext>();
        var tenantRepo = context.RequestServices.GetRequiredService<ITenantRepository>();

        // Strategy 1: X-Tenant-ID header
        if (context.Request.Headers.TryGetValue("X-Tenant-ID", out var tenantIdHeader)
            && Guid.TryParse(tenantIdHeader.FirstOrDefault(), out var tenantId))
        {
            var tenant = await tenantRepo.GetByIdAsync(tenantId);
            if (tenant != null && tenant.IsActive)
            {
                tenantContext.SetTenant(tenant.Id, tenant.Slug);
                await _next(context);
                return;
            }
        }

        // Strategy 2: X-Tenant-Slug header
        if (context.Request.Headers.TryGetValue("X-Tenant-Slug", out var slugHeader))
        {
            var slug = slugHeader.FirstOrDefault();
            if (!string.IsNullOrEmpty(slug))
            {
                var tenant = await tenantRepo.GetBySlugAsync(slug);
                if (tenant != null && tenant.IsActive)
                {
                    tenantContext.SetTenant(tenant.Id, tenant.Slug);
                    await _next(context);
                    return;
                }
            }
        }

        // Strategy 3: Subdomain
        var host = context.Request.Host.Host;
        var parts = host.Split('.');
        if (parts.Length >= 3)
        {
            var subdomain = parts[0];
            if (subdomain != "www" && subdomain != "api")
            {
                var tenant = await tenantRepo.GetBySlugAsync(subdomain);
                if (tenant != null && tenant.IsActive)
                {
                    tenantContext.SetTenant(tenant.Id, tenant.Slug);
                    await _next(context);
                    return;
                }
            }
        }

        // Strategy 4: JWT claim
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var tenantClaim = context.User.FindFirst("tenant_id")?.Value;
            if (Guid.TryParse(tenantClaim, out var jwtTenantId))
            {
                var tenant = await tenantRepo.GetByIdAsync(jwtTenantId);
                if (tenant != null && tenant.IsActive)
                {
                    tenantContext.SetTenant(tenant.Id, tenant.Slug);
                    await _next(context);
                    return;
                }
            }
        }

        // No tenant resolved
        context.Response.StatusCode = 400;
        await context.Response.WriteAsJsonAsync(new
        {
            error = "Tenant could not be resolved. Provide X-Tenant-ID or X-Tenant-Slug header."
        });
    }
}
