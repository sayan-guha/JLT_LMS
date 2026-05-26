using JLT.Domain.Common;

namespace JLT.Domain.Entities;

public class UserRole : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual User? User { get; set; }
    public virtual Role? Role { get; set; }
}
