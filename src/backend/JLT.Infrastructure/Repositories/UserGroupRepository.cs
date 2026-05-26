using System.Text.Json;
using JLT.Domain.Entities;
using JLT.Domain.Interfaces;
using JLT.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JLT.Infrastructure.Repositories;

public class UserGroupRepository : GenericRepository<UserGroup>, IUserGroupRepository
{
    public UserGroupRepository(AppDbContext context) : base(context) { }

    public async Task<UserGroup?> GetWithMembersAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(g => g.Members)
                .ThenInclude(m => m.User)
            .Include(g => g.CreatedBy)
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);
    }

    public async Task<IReadOnlyList<User>> EvaluateDynamicGroupRulesAsync(string rulesJson, CancellationToken cancellationToken = default)
    {
        // Rules are JSON objects like:
        // { "department": "Engineering", "location": "NYC", "attributes.cost_center": "CC-100" }
        // We convert these to a LINQ query dynamically

        var rules = JsonSerializer.Deserialize<Dictionary<string, string>>(rulesJson)
            ?? new Dictionary<string, string>();

        IQueryable<User> query = _context.Users;

        foreach (var rule in rules)
        {
            var key = rule.Key;
            var value = rule.Value;

            if (key.StartsWith("attributes."))
            {
                // JSONB attribute filter — use raw SQL fragment
                var jsonKey = key["attributes.".Length..];
                var jsonFilter = $"{{\"{jsonKey}\": \"{value}\"}}";
                query = query.Where(u =>
                    EF.Functions.JsonContains(u.Attributes!, jsonFilter));
            }
            else
            {
                // Standard property filter
                query = key switch
                {
                    "department" => query.Where(u => u.Department == value),
                    "location" => query.Where(u => u.Location == value),
                    "jobTitle" => query.Where(u => u.JobTitle == value),
                    "isActive" => query.Where(u => u.IsActive == bool.Parse(value)),
                    _ => query
                };
            }
        }

        return await query.AsNoTracking().ToListAsync(cancellationToken);
    }
}
