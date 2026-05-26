using JLT.Domain.Common;

namespace JLT.Domain.Entities;

public class UserGroupMember : BaseEntity
{
    public Guid GroupId { get; set; }
    public Guid UserId { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual UserGroup? Group { get; set; }
    public virtual User? User { get; set; }
}
