using JLT.Domain.Common;

namespace JLT.Domain.Entities;

public class RolePermission : BaseEntity
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }

    // Navigation
    public virtual Role? Role { get; set; }
    public virtual Permission? Permission { get; set; }
}
