using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JLT.Application.DTOs;
using JLT.Domain.Entities;
using JLT.Domain.Enums;
using JLT.Domain.Interfaces;
using MediatR;

namespace JLT.Application.Features.LearningContent;

// =============================================================================
// Upsert Progress
// =============================================================================
public record UpsertContentProgressCommand(
    Guid UserId,
    Guid LearningContentId,
    decimal ProgressPercent,
    string? BookmarkData,
    int TimeSpentSeconds) : IRequest<ContentProgressDto>;

public class UpsertContentProgressHandler : IRequestHandler<UpsertContentProgressCommand, ContentProgressDto>
{
    private readonly IContentProgressRepository _repo;
    public UpsertContentProgressHandler(IContentProgressRepository repo) => _repo = repo;

    public async Task<ContentProgressDto> Handle(UpsertContentProgressCommand request, CancellationToken ct)
    {
        var existing = await _repo.GetByUserAndContentAsync(request.UserId, request.LearningContentId, ct);

        if (existing == null)
        {
            var entity = new ContentProgress
            {
                UserId = request.UserId,
                LearningContentId = request.LearningContentId,
                ProgressPercent = request.ProgressPercent,
                BookmarkData = request.BookmarkData,
                TimeSpentSeconds = request.TimeSpentSeconds,
                LastAccessedAt = DateTime.UtcNow,
                Status = request.ProgressPercent >= 100 ? ProgressStatus.Completed
                       : request.ProgressPercent > 0 ? ProgressStatus.InProgress
                       : ProgressStatus.NotStarted,
                CompletedAt = request.ProgressPercent >= 100 ? DateTime.UtcNow : null
            };

            await _repo.AddAsync(entity, ct);
            return ToDto(entity);
        }
        else
        {
            existing.ProgressPercent = request.ProgressPercent;
            existing.BookmarkData = request.BookmarkData;
            existing.TimeSpentSeconds = request.TimeSpentSeconds;
            existing.LastAccessedAt = DateTime.UtcNow;

            if (request.ProgressPercent >= 100 && existing.Status != ProgressStatus.Completed)
            {
                existing.Status = ProgressStatus.Completed;
                existing.CompletedAt = DateTime.UtcNow;
            }
            else if (request.ProgressPercent > 0 && existing.Status == ProgressStatus.NotStarted)
            {
                existing.Status = ProgressStatus.InProgress;
            }

            await _repo.UpdateAsync(existing, ct);
            return ToDto(existing);
        }
    }

    private static ContentProgressDto ToDto(ContentProgress p) => new(
        p.Id, p.UserId, p.LearningContentId,
        p.LearningContent?.Title,
        p.Status.ToString(), p.ProgressPercent,
        p.BookmarkData, p.TimeSpentSeconds,
        p.CompletedAt, p.LastAccessedAt);
}

// =============================================================================
// Get Progress (single)
// =============================================================================
public record GetContentProgressQuery(Guid UserId, Guid LearningContentId) : IRequest<ContentProgressDto?>;

public class GetContentProgressHandler : IRequestHandler<GetContentProgressQuery, ContentProgressDto?>
{
    private readonly IContentProgressRepository _repo;
    public GetContentProgressHandler(IContentProgressRepository repo) => _repo = repo;

    public async Task<ContentProgressDto?> Handle(GetContentProgressQuery request, CancellationToken ct)
    {
        var p = await _repo.GetByUserAndContentAsync(request.UserId, request.LearningContentId, ct);
        if (p == null) return null;
        return new ContentProgressDto(
            p.Id, p.UserId, p.LearningContentId,
            p.LearningContent?.Title,
            p.Status.ToString(), p.ProgressPercent,
            p.BookmarkData, p.TimeSpentSeconds,
            p.CompletedAt, p.LastAccessedAt);
    }
}

// =============================================================================
// Get User Progress List
// =============================================================================
public record GetUserProgressListQuery(Guid UserId) : IRequest<List<ContentProgressDto>>;

public class GetUserProgressListHandler : IRequestHandler<GetUserProgressListQuery, List<ContentProgressDto>>
{
    private readonly IContentProgressRepository _repo;
    public GetUserProgressListHandler(IContentProgressRepository repo) => _repo = repo;

    public async Task<List<ContentProgressDto>> Handle(GetUserProgressListQuery request, CancellationToken ct)
    {
        var records = await _repo.GetByUserAsync(request.UserId, ct);
        return records.Select(p => new ContentProgressDto(
            p.Id, p.UserId, p.LearningContentId,
            p.LearningContent?.Title,
            p.Status.ToString(), p.ProgressPercent,
            p.BookmarkData, p.TimeSpentSeconds,
            p.CompletedAt, p.LastAccessedAt)).ToList();
    }
}
