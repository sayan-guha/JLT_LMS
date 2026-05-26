using JLT.Domain.Entities;
using JLT.Domain.Enums;
using JLT.Domain.Interfaces;
using JLT.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JLT.Infrastructure.Repositories;

public class LearningContentRepository : GenericRepository<LearningContent>, ILearningContentRepository
{
    public LearningContentRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<LearningContent>> GetByContentTypeAsync(ContentType type, CancellationToken ct = default)
    {
        return await _dbSet.Where(c => c.ContentType == type).AsNoTracking().ToListAsync(ct);
    }

    public async Task<IReadOnlyList<LearningContent>> GetByStatusAsync(ContentStatus status, CancellationToken ct = default)
    {
        return await _dbSet.Where(c => c.Status == status).AsNoTracking().ToListAsync(ct);
    }

    public async Task<IReadOnlyList<LearningContent>> GetExpiredContentAsync(DateTime now, CancellationToken ct = default)
    {
        return await _dbSet
            .Where(c => c.ValidTill.HasValue && c.ValidTill.Value <= now && c.Status == ContentStatus.Published)
            .ToListAsync(ct);
    }
}
