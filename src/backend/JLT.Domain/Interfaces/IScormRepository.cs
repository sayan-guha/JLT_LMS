using JLT.Domain.Entities;

namespace JLT.Domain.Interfaces;

public interface IScormRepository
{
    // ScormPackage
    Task<ScormPackage?> GetPackageByContentIdAsync(Guid learningContentId, CancellationToken ct = default);
    Task<ScormPackage> AddPackageAsync(ScormPackage package, CancellationToken ct = default);

    // ScormRuntimeState
    Task<ScormRuntimeState?> GetRuntimeStateAsync(Guid userId, Guid scormPackageId, CancellationToken ct = default);
    Task<ScormRuntimeState> UpsertRuntimeStateAsync(ScormRuntimeState state, CancellationToken ct = default);
}
