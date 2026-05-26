using JLT.Domain.Common;
using JLT.Domain.Enums;

namespace JLT.Domain.Entities;

public class UserGroup : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public GroupType Type { get; set; } = GroupType.Static;
    public string? Rules { get; set; }
    public Guid? CreatedById { get; set; }

    // Navigation
    public virtual User? CreatedBy { get; set; }
    public virtual ICollection<UserGroupMember> Members { get; set; } = new List<UserGroupMember>();
}
