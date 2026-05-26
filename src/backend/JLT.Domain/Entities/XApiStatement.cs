using JLT.Domain.Common;

namespace JLT.Domain.Entities;

public class XApiStatement : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string ActorJson { get; set; } = "{}";       // JSONB
    public string VerbId { get; set; } = string.Empty;
    public string ObjectJson { get; set; } = "{}";       // JSONB
    public string? ResultJson { get; set; }               // JSONB
    public string? ContextJson { get; set; }              // JSONB
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public DateTime StoredAt { get; set; } = DateTime.UtcNow;
}
