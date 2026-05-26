using MediatR;

namespace JLT.Domain.Events;

public record UserAttributesUpdatedEvent(Guid UserId, Guid TenantId) : INotification;
