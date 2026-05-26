using JLT.Domain.Entities;

namespace JLT.Domain.Interfaces;

public interface IUserGroupRepository : IRepository<UserGroup>
{
    Task<UserGroup?> GetWithMembersAsync(Guid groupId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> EvaluateDynamicGroupRulesAsync(string rulesJson, CancellationToken cancellationToken = default);
}
