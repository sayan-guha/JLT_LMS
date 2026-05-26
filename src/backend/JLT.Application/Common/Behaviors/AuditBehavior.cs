using System;
using System.Threading;
using System.Threading.Tasks;
using JLT.Domain.Enums;
using JLT.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace JLT.Application.Common.Behaviors;

public interface IAuditableRequest
{
    string AuditAction { get; }
    string AuditEntityType { get; }
    Guid? GetAuditEntityId(object? response);
}

public class AuditBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IAuditService _auditService;
    private readonly ILogger<AuditBehavior<TRequest, TResponse>> _logger;

    public AuditBehavior(IAuditService auditService, ILogger<AuditBehavior<TRequest, TResponse>> logger)
    {
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next();

        if (request is IAuditableRequest auditableRequest)
        {
            try
            {
                var entityId = auditableRequest.GetAuditEntityId(response);
                await _auditService.LogAsync(
                    auditableRequest.AuditAction,
                    auditableRequest.AuditEntityType,
                    entityId,
                    oldValues: null,
                    newValues: request,
                    source: AuditSource.User,
                    cancellationToken: cancellationToken
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write audit log for request {RequestType}", typeof(TRequest).Name);
            }
        }

        return response;
    }
}
