using JLT.Domain.Entities;

namespace JLT.Domain.Interfaces;

public interface IDynamicFieldService
{
    Task<IReadOnlyList<DynamicFieldDefinition>> GetFieldDefinitionsAsync(CancellationToken cancellationToken = default);
    Task ValidateAttributesAsync(string? attributesJson, CancellationToken cancellationToken = default);
}
