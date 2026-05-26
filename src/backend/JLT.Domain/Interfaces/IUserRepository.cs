using JLT.Domain.Entities;

namespace JLT.Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetByAttributesAsync(string attributesJson, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetByDepartmentAsync(string department, CancellationToken cancellationToken = default);
    Task<int> BulkUpdateStatusAsync(IEnumerable<Guid> userIds, bool isActive, CancellationToken cancellationToken = default);
    Task<int> BulkUpdateAttributesAsync(IEnumerable<Guid> userIds, string attributesJson, CancellationToken cancellationToken = default);
    Task<RefreshToken?> GetRefreshTokenWithUserAsync(string token, CancellationToken cancellationToken = default);
    Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
    Task UpdateRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
}
