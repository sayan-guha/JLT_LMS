using JLT.Domain.Entities;
using JLT.Domain.Enums;

namespace JLT.Domain.Interfaces;

public interface ILearningContentRepository : IRepository<LearningContent>
{
    Task<IReadOnlyList<LearningContent>> GetByContentTypeAsync(ContentType type, CancellationToken ct = default);
    Task<IReadOnlyList<LearningContent>> GetByStatusAsync(ContentStatus status, CancellationToken ct = default);
    Task<IReadOnlyList<LearningContent>> GetExpiredContentAsync(DateTime now, CancellationToken ct = default);
}
