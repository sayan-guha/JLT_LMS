using MediatR;

namespace JLT.Domain.Events;

public record UserDeactivatedEvent(Guid UserId, Guid TenantId) : INotification;
