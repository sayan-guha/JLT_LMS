using System.Text.Json;
using JLT.Domain.Entities;
using JLT.Domain.Enums;
using JLT.Domain.Interfaces;
using JLT.Infrastructure.Persistence;
using JLT.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace JLT.Infrastructure.Services;

public class DynamicFieldService : IDynamicFieldService
{
    private readonly AppDbContext _context;
    private readonly ITenantContext _tenantContext;

    public DynamicFieldService(AppDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<DynamicFieldDefinition>> GetFieldDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.DynamicFieldDefinitions
            .Where(d => d.IsActive)
            .OrderBy(d => d.SortOrder)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task ValidateAttributesAsync(string? attributesJson, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(attributesJson))
            return;

        var definitions = await GetFieldDefinitionsAsync(cancellationToken);
        var attributes = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(attributesJson)
            ?? new Dictionary<string, JsonElement>();

        var errors = new List<string>();

        // Check required fields are present
        foreach (var def in definitions.Where(d => d.IsRequired))
        {
            if (!attributes.ContainsKey(def.FieldKey) ||
                attributes[def.FieldKey].ValueKind == JsonValueKind.Null ||
                (attributes[def.FieldKey].ValueKind == JsonValueKind.String
                    && string.IsNullOrWhiteSpace(attributes[def.FieldKey].GetString())))
            {
                errors.Add($"Required field '{def.DisplayName}' ({def.FieldKey}) is missing.");
            }
        }

        // Validate field types and values
        foreach (var (key, value) in attributes)
        {
            var definition = definitions.FirstOrDefault(d => d.FieldKey == key);
            if (definition == null)
            {
                errors.Add($"Unknown field '{key}'. It is not defined for this tenant.");
                continue;
            }

            if (value.ValueKind == JsonValueKind.Null)
                continue;

            switch (definition.FieldType)
            {
                case DynamicFieldType.Number:
                    if (value.ValueKind != JsonValueKind.Number)
                        errors.Add($"Field '{key}' must be a number.");
                    break;

                case DynamicFieldType.Boolean:
                    if (value.ValueKind != JsonValueKind.True && value.ValueKind != JsonValueKind.False)
                        errors.Add($"Field '{key}' must be a boolean.");
                    break;

                case DynamicFieldType.Date:
                    if (value.ValueKind != JsonValueKind.String || !DateTime.TryParse(value.GetString(), out _))
                        errors.Add($"Field '{key}' must be a valid date string.");
                    break;

                case DynamicFieldType.Dropdown:
                    if (value.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(definition.Options))
                    {
                        var options = JsonSerializer.Deserialize<List<string>>(definition.Options) ?? new();
                        if (!options.Contains(value.GetString()!))
                            errors.Add($"Field '{key}' must be one of: {string.Join(", ", options)}.");
                    }
                    break;

                case DynamicFieldType.MultiSelect:
                    if (value.ValueKind == JsonValueKind.Array && !string.IsNullOrEmpty(definition.Options))
                    {
                        var validOptions = JsonSerializer.Deserialize<List<string>>(definition.Options) ?? new();
                        var selected = value.EnumerateArray().Select(v => v.GetString()!).ToList();
                        var invalid = selected.Except(validOptions).ToList();
                        if (invalid.Any())
                            errors.Add($"Field '{key}' has invalid values: {string.Join(", ", invalid)}.");
                    }
                    break;
            }
        }

        if (errors.Any())
        {
            throw new ValidationException(errors);
        }
    }
}

/// <summary>
/// Thrown when dynamic field validation fails.
/// </summary>
public class ValidationException : Exception
{
    public IReadOnlyList<string> Errors { get; }

    public ValidationException(IReadOnlyList<string> errors)
        : base($"Validation failed: {string.Join("; ", errors)}")
    {
        Errors = errors;
    }
}
