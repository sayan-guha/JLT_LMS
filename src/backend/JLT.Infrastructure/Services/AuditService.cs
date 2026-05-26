using System.Text.Json;
using JLT.Domain.Entities;
using JLT.Domain.Enums;
using JLT.Domain.Interfaces;
using JLT.Infrastructure.Persistence;
using JLT.MultiTenancy;
using Microsoft.AspNetCore.Http;

namespace JLT.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly AppDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(AppDbContext context, ITenantContext tenantContext, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _tenantContext = tenantContext;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(string action, string? entityType = null, Guid? entityId = null,
        object? oldValues = null, object? newValues = null,
        AuditSource source = AuditSource.User, CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var userId = httpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var auditLog = new AuditLog
        {
            TenantId = _tenantContext.TenantId,
            UserId = Guid.TryParse(userId, out var uid) ? uid : null,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
            NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
            IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString(),
            UserAgent = httpContext?.Request.Headers.UserAgent.ToString(),
            Source = source,
            CreatedAt = DateTime.UtcNow
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
