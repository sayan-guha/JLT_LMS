using JLT.Domain.Entities;
using JLT.Domain.Interfaces;
using JLT.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JLT.Infrastructure.Repositories;

public class ScormRepository : IScormRepository
{
    private readonly AppDbContext _context;

    public ScormRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ScormPackage?> GetPackageByContentIdAsync(Guid learningContentId, CancellationToken ct = default)
    {
        return await _context.ScormPackages
            .Include(s => s.RuntimeStates)
            .FirstOrDefaultAsync(s => s.LearningContentId == learningContentId, ct);
    }

    public async Task<ScormPackage> AddPackageAsync(ScormPackage package, CancellationToken ct = default)
    {
        await _context.ScormPackages.AddAsync(package, ct);
        await _context.SaveChangesAsync(ct);
        return package;
    }

    public async Task<ScormRuntimeState?> GetRuntimeStateAsync(Guid userId, Guid scormPackageId, CancellationToken ct = default)
    {
        return await _context.ScormRuntimeStates
            .FirstOrDefaultAsync(r => r.UserId == userId && r.ScormPackageId == scormPackageId, ct);
    }

    public async Task<ScormRuntimeState> UpsertRuntimeStateAsync(ScormRuntimeState state, CancellationToken ct = default)
    {
        var existing = await GetRuntimeStateAsync(state.UserId, state.ScormPackageId, ct);
        if (existing == null)
        {
            await _context.ScormRuntimeStates.AddAsync(state, ct);
        }
        else
        {
            existing.LessonStatus = state.LessonStatus;
            existing.LessonLocation = state.LessonLocation;
            existing.SuspendData = state.SuspendData;
            existing.RawScore = state.RawScore;
            existing.MinScore = state.MinScore;
            existing.MaxScore = state.MaxScore;
            existing.SessionTime = state.SessionTime;
            existing.TotalTime = state.TotalTime;
            existing.Entry = state.Entry;
        }
        await _context.SaveChangesAsync(ct);
        return existing ?? state;
    }
}
