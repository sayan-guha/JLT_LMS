using JLT.Domain.Entities;
using JLT.Domain.Interfaces;
using JLT.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JLT.Infrastructure.Repositories;

public class ContentProgressRepository : GenericRepository<ContentProgress>, IContentProgressRepository
{
    public ContentProgressRepository(AppDbContext context) : base(context) { }

    public async Task<ContentProgress?> GetByUserAndContentAsync(Guid userId, Guid contentId, CancellationToken ct = default)
    {
        return await _dbSet.FirstOrDefaultAsync(p => p.UserId == userId && p.LearningContentId == contentId, ct);
    }

    public async Task<IReadOnlyList<ContentProgress>> GetByUserAsync(Guid userId, CancellationToken ct = default)
    {
        return await _dbSet.Where(p => p.UserId == userId).Include(p => p.LearningContent).AsNoTracking().ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ContentProgress>> GetByContentAsync(Guid contentId, CancellationToken ct = default)
    {
        return await _dbSet.Where(p => p.LearningContentId == contentId).AsNoTracking().ToListAsync(ct);
    }
}
