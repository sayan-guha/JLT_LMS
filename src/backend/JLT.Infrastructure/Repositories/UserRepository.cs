using JLT.Domain.Entities;
using JLT.Domain.Interfaces;
using JLT.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JLT.Infrastructure.Repositories;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public override async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetByAttributesAsync(string attributesJson, CancellationToken cancellationToken = default)
    {
        // Use PostgreSQL JSONB containment operator: attributes @> '{"department": "Engineering"}'
        return await _dbSet
            .FromSqlInterpolated($"SELECT * FROM users WHERE attributes @> {attributesJson}::jsonb")
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetByDepartmentAsync(string department, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(u => u.Department == department)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<int> BulkUpdateStatusAsync(IEnumerable<Guid> userIds, bool isActive, CancellationToken cancellationToken = default)
    {
        var ids = userIds.ToList();
        return await _dbSet
            .Where(u => ids.Contains(u.Id))
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.IsActive, isActive)
                .SetProperty(u => u.UpdatedAt, DateTime.UtcNow),
                cancellationToken);
    }

    public async Task<int> BulkUpdateAttributesAsync(IEnumerable<Guid> userIds, string attributesJson, CancellationToken cancellationToken = default)
    {
        var ids = userIds.ToList();
        // Merge new attributes into existing ones using PostgreSQL JSONB concat
        return await _context.Database
            .ExecuteSqlInterpolatedAsync(
                $"UPDATE users SET attributes = COALESCE(attributes, '{{}}'::jsonb) || {attributesJson}::jsonb, updated_at = NOW() WHERE id = ANY({ids})",
                cancellationToken);
    }

    public async Task<RefreshToken?> GetRefreshTokenWithUserAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _context.Set<RefreshToken>()
            .Include(rt => rt.User)
                .ThenInclude(u => u!.UserRoles)
                    .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);
    }

    public async Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        await _context.Set<RefreshToken>().AddAsync(refreshToken, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        _context.Entry(refreshToken).State = EntityState.Modified;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
