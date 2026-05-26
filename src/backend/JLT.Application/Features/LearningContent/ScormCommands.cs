using System;
using System.Threading;
using System.Threading.Tasks;
using JLT.Application.DTOs;
using JLT.Domain.Entities;
using JLT.Domain.Interfaces;
using MediatR;

namespace JLT.Application.Features.LearningContent;

// =============================================================================
// Get SCORM Runtime State
// =============================================================================
public record GetScormRuntimeStateQuery(Guid UserId, Guid LearningContentId) : IRequest<ScormRuntimeStateDto?>;

public class GetScormRuntimeStateHandler : IRequestHandler<GetScormRuntimeStateQuery, ScormRuntimeStateDto?>
{
    private readonly IScormRepository _repo;
    public GetScormRuntimeStateHandler(IScormRepository repo) => _repo = repo;

    public async Task<ScormRuntimeStateDto?> Handle(GetScormRuntimeStateQuery request, CancellationToken ct)
    {
        var package = await _repo.GetPackageByContentIdAsync(request.LearningContentId, ct);
        if (package == null) return null;

        var state = await _repo.GetRuntimeStateAsync(request.UserId, package.Id, ct);
        if (state == null) return null;

        return ToDto(state);
    }

    private static ScormRuntimeStateDto ToDto(ScormRuntimeState s) => new(
        s.Id, s.UserId, s.ScormPackageId,
        s.LessonStatus, s.LessonLocation, s.SuspendData,
        s.RawScore, s.MinScore, s.MaxScore,
        s.SessionTime, s.TotalTime, s.Entry);
}

// =============================================================================
// Upsert SCORM Runtime State
// =============================================================================
public record UpsertScormRuntimeStateCommand(
    Guid UserId,
    Guid LearningContentId,
    string LessonStatus,
    string? LessonLocation = null,
    string? SuspendData = null,
    decimal? RawScore = null,
    decimal? MinScore = null,
    decimal? MaxScore = null,
    string? SessionTime = null,
    string? TotalTime = null,
    string? Entry = null) : IRequest<ScormRuntimeStateDto>;

public class UpsertScormRuntimeStateHandler : IRequestHandler<UpsertScormRuntimeStateCommand, ScormRuntimeStateDto>
{
    private readonly IScormRepository _repo;
    public UpsertScormRuntimeStateHandler(IScormRepository repo) => _repo = repo;

    public async Task<ScormRuntimeStateDto> Handle(UpsertScormRuntimeStateCommand request, CancellationToken ct)
    {
        var package = await _repo.GetPackageByContentIdAsync(request.LearningContentId, ct)
            ?? throw new InvalidOperationException("SCORM package not found for this content.");

        var state = new ScormRuntimeState
        {
            UserId = request.UserId,
            ScormPackageId = package.Id,
            LessonStatus = request.LessonStatus,
            LessonLocation = request.LessonLocation,
            SuspendData = request.SuspendData,
            RawScore = request.RawScore,
            MinScore = request.MinScore,
            MaxScore = request.MaxScore,
            SessionTime = request.SessionTime,
            TotalTime = request.TotalTime,
            Entry = request.Entry
        };

        var result = await _repo.UpsertRuntimeStateAsync(state, ct);

        return new ScormRuntimeStateDto(
            result.Id, result.UserId, result.ScormPackageId,
            result.LessonStatus, result.LessonLocation, result.SuspendData,
            result.RawScore, result.MinScore, result.MaxScore,
            result.SessionTime, result.TotalTime, result.Entry);
    }
}
