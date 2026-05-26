using JLT.Domain.Entities;

namespace JLT.Domain.Interfaces;

public interface IXApiStatementRepository : IRepository<XApiStatement>
{
    Task<IReadOnlyList<XApiStatement>> GetByVerbAsync(string verbId, CancellationToken ct = default);
    Task<IReadOnlyList<XApiStatement>> GetByActorAsync(string actorJson, CancellationToken ct = default);
}
