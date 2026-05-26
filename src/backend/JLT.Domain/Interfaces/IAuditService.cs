using JLT.Domain.Enums;

namespace JLT.Domain.Interfaces;

public interface IAuditService
{
    Task LogAsync(string action, string? entityType = null, Guid? entityId = null,
        object? oldValues = null, object? newValues = null,
        AuditSource source = AuditSource.User, CancellationToken cancellationToken = default);
}
