using JLT.Domain.Common;
using JLT.Domain.Enums;

namespace JLT.Domain.Entities;

public class DynamicFieldDefinition : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string FieldKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DynamicFieldType FieldType { get; set; } = DynamicFieldType.Text;
    public bool IsRequired { get; set; }
    public string? Options { get; set; }
    public string? DefaultValue { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
