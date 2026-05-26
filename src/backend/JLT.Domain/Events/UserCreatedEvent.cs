using MediatR;

namespace JLT.Domain.Events;

public record UserCreatedEvent(Guid UserId, Guid TenantId) : INotification;
