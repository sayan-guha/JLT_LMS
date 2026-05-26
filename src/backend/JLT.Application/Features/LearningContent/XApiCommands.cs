using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JLT.Application.Common.Models;
using JLT.Application.DTOs;
using JLT.Domain.Entities;
using JLT.Domain.Interfaces;
using MediatR;

namespace JLT.Application.Features.LearningContent;

// =============================================================================
// Store xAPI Statement
// =============================================================================
public record StoreXApiStatementCommand(
    string ActorJson,
    string VerbId,
    string ObjectJson,
    string? ResultJson = null,
    string? ContextJson = null,
    DateTime? Timestamp = null) : IRequest<XApiStatementDto>;

public class StoreXApiStatementHandler : IRequestHandler<StoreXApiStatementCommand, XApiStatementDto>
{
    private readonly IXApiStatementRepository _repo;
    public StoreXApiStatementHandler(IXApiStatementRepository repo) => _repo = repo;

    public async Task<XApiStatementDto> Handle(StoreXApiStatementCommand request, CancellationToken ct)
    {
        var statement = new XApiStatement
        {
            ActorJson = request.ActorJson,
            VerbId = request.VerbId,
            ObjectJson = request.ObjectJson,
            ResultJson = request.ResultJson,
            ContextJson = request.ContextJson,
            Timestamp = request.Timestamp ?? DateTime.UtcNow,
            StoredAt = DateTime.UtcNow
        };

        await _repo.AddAsync(statement, ct);

        return new XApiStatementDto(
            statement.Id, statement.ActorJson, statement.VerbId,
            statement.ObjectJson, statement.ResultJson, statement.ContextJson,
            statement.Timestamp, statement.StoredAt);
    }
}

// =============================================================================
// Get xAPI Statements (paginated)
// =============================================================================
public record GetXApiStatementsQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? VerbId = null,
    string? ActorJson = null) : IRequest<PaginatedList<XApiStatementDto>>;

public class GetXApiStatementsHandler : IRequestHandler<GetXApiStatementsQuery, PaginatedList<XApiStatementDto>>
{
    private readonly IXApiStatementRepository _repo;
    public GetXApiStatementsHandler(IXApiStatementRepository repo) => _repo = repo;

    public async Task<PaginatedList<XApiStatementDto>> Handle(GetXApiStatementsQuery request, CancellationToken ct)
    {
        var query = _repo.Query().OrderByDescending(x => x.Timestamp);

        IQueryable<XApiStatement> filtered = query;

        if (!string.IsNullOrWhiteSpace(request.VerbId))
            filtered = filtered.Where(x => x.VerbId == request.VerbId);

        if (!string.IsNullOrWhiteSpace(request.ActorJson))
            filtered = filtered.Where(x => x.ActorJson.Contains(request.ActorJson));

        var paged = await PaginatedList<XApiStatement>.CreateAsync(filtered, request.PageNumber, request.PageSize, ct);

        var items = paged.Items.Select(x => new XApiStatementDto(
            x.Id, x.ActorJson, x.VerbId, x.ObjectJson,
            x.ResultJson, x.ContextJson, x.Timestamp, x.StoredAt)).ToList();

        return new PaginatedList<XApiStatementDto>(items, paged.TotalCount, paged.PageIndex, request.PageSize);
    }
}
