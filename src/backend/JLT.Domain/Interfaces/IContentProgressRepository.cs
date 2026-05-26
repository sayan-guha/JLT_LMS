using JLT.Domain.Entities;

namespace JLT.Domain.Interfaces;

public interface IContentProgressRepository : IRepository<ContentProgress>
{
    Task<ContentProgress?> GetByUserAndContentAsync(Guid userId, Guid contentId, CancellationToken ct = default);
    Task<IReadOnlyList<ContentProgress>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<ContentProgress>> GetByContentAsync(Guid contentId, CancellationToken ct = default);
}
