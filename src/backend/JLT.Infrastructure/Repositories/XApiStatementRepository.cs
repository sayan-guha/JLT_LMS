using JLT.Domain.Entities;
using JLT.Domain.Interfaces;
using JLT.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JLT.Infrastructure.Repositories;

public class XApiStatementRepository : GenericRepository<XApiStatement>, IXApiStatementRepository
{
    public XApiStatementRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<XApiStatement>> GetByVerbAsync(string verbId, CancellationToken ct = default)
    {
        return await _dbSet.Where(x => x.VerbId == verbId).AsNoTracking().OrderByDescending(x => x.Timestamp).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<XApiStatement>> GetByActorAsync(string actorJson, CancellationToken ct = default)
    {
        return await _dbSet.Where(x => EF.Functions.JsonContains(x.ActorJson, actorJson)).AsNoTracking().OrderByDescending(x => x.Timestamp).ToListAsync(ct);
    }
}
